using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Models;
using Nancy;
using Nancy.Testing;
using Xunit;

namespace Fabric.Authorization.IntegrationTests.Modules
{
    [Collection("InMemoryTests")]
    public class GroupsTests : IClassFixture<IntegrationTestsFixture>
    {
        protected readonly Browser Browser;
        private readonly DefaultPropertySettings _defaultPropertySettings;
        protected ClaimsPrincipal Principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
        {
            new Claim(Claims.Scope, Scopes.ManageClientsScope),
            new Claim(Claims.Scope, Scopes.ReadScope),
            new Claim(Claims.Scope, Scopes.WriteScope),
            new Claim(Claims.ClientId, "rolesprincipal"),
            new Claim(Claims.IdentityProvider, "idP1")
        }, "rolesprincipal"));

        public GroupsTests(IntegrationTestsFixture fixture, string storageProvider = StorageProviders.InMemory)
        {
            Browser = fixture.GetBrowser(Principal, storageProvider);
            _defaultPropertySettings = fixture.DefaultPropertySettings;
            fixture.CreateClient(Browser, "rolesprincipal");
        }

        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodName]
        [InlineData("NonexistentGroup")]
        [InlineData("NonexistentGroup2")]
        public void GetGroup_NonexistentGroup_NotFound(string groupName)
        {
            var get = Browser.Get($"/groups/{groupName}", with =>
            {
                with.HttpRequest();
            }).Result;

            Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        }

        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodName]
        [InlineData("Group1", "Source1")]
        [InlineData("Group2", "Source2")]
        [InlineData("6BC32347-36A1-44CF-AA0E-6C1038AA1DF3", "Source3")]
        public void AddGroup_SingleGroup_Success(string groupName, string groupSource)
        {
            var postResponse = Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = groupName,
                    GroupSource = groupSource
                });
            }).Result;

            var getResponse = Browser.Get($"/groups/{groupName}", with =>
            {
                with.HttpRequest();
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.Contains(groupName, getResponse.Body.AsString());
        }

        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodName]
        [InlineData("BatchGroup1", "BatchSource1")]
        [InlineData("BatchGroup2", "BatchSource2")]
        [InlineData("6AC32A47-36C1-23BF-AA22-6C1028AA5DC3", "BatchSource3")]
        public void AddGroup_Batch_Success(string groupName, string groupSource)
        {
            groupName = groupName + Guid.NewGuid();
            var postResponse = Browser.Post("/groups/UpdateGroups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new []
                {
                    new
                    {
                        Id = groupName + "_0",
                        GroupName = groupName + "_0",
                        GroupSource = groupSource + "_0"
                    },
                    new
                    {
                        Id = groupName + "_1",
                        GroupName = groupName + "_1",
                        GroupSource = groupSource + "_1"
                    },
                    new
                    {
                        Id = groupName + "_2",
                        GroupName = groupName + "_2",
                        GroupSource = groupSource + "_2"
                    }
                });
            }).Result;

            Assert.Equal(HttpStatusCode.NoContent, postResponse.StatusCode);

            var getResponse0 = Browser.Get($"/groups/{groupName}_0", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            var getResponse1 = Browser.Get($"/groups/{groupName}_1", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            var getResponse2 = Browser.Get($"/groups/{groupName}_2", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.OK, getResponse0.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse1.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse2.StatusCode);

            Assert.Contains(groupName + "_0", getResponse0.Body.AsString());
            Assert.Contains(groupName + "_1", getResponse1.Body.AsString());
            Assert.Contains(groupName + "_2", getResponse2.Body.AsString());

            Assert.Contains(groupSource + "_0", getResponse0.Body.AsString());
            Assert.Contains(groupSource + "_1", getResponse1.Body.AsString());
            Assert.Contains(groupSource + "_2", getResponse2.Body.AsString());
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public void AddGroup_DuplicateGroupExistsAndDeleted_Success()
        {
            string groupName = "Group1" + Guid.NewGuid();
            const string groupSource = "Custom";
            var response = Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = groupName,
                    GroupSource = groupSource
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            response = Browser.Delete($"/groups/{groupName}", with =>
            {
                with.HttpRequest();
            }).Result;

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            response = Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = groupName,
                    GroupSource = groupSource
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodName]
        [InlineData("", "Source1")]
        [InlineData(null, "Source2")]
        public void AddGroup_NullOrEmptyName_BadRequest(string groupName, string groupSource)
        {
            var postResponse = Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = groupName,
                    GroupSource = groupSource
                });
            }).Result;

            Assert.Equal(HttpStatusCode.BadRequest, postResponse.StatusCode);
        }

        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodName]
        [InlineData("Source1")]
        public void AddGroup_MissingName_BadRequest(string groupSource)
        {
            var postResponse = Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupSource = groupSource
                });
            }).Result;

            Assert.Equal(HttpStatusCode.BadRequest, postResponse.StatusCode);
        }

        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodName]
        [InlineData("Source1", "")]
        [InlineData("Source2", null)]
        public void AddGroup_NullOrEmptySource_Success(string groupName, string groupSource)
        {
            groupName = groupName + Guid.NewGuid();
            var postResponse = Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = groupName,
                    GroupSource = groupSource
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            var getResponse = Browser.Get($"/groups/{groupName}", with =>
            {
                with.HttpRequest();
            }).Result;

            var source = getResponse.Body.DeserializeJson<GroupRoleApiModel>().GroupSource;
            Assert.Equal(_defaultPropertySettings.GroupSource, source);
        }

        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodName]
        [InlineData("Name1")]
        public void AddGroup_MissingSource_Success(string groupName)
        {
            var postResponse = Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = groupName
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            var getResponse = Browser.Get($"/groups/{groupName}", with =>
            {
                with.HttpRequest();
            }).Result;

            var source = getResponse.Body.DeserializeJson<GroupRoleApiModel>().GroupSource;
            Assert.Equal(_defaultPropertySettings.GroupSource, source);
        }

        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodName]
        [InlineData("RepeatedGroup1", "Custom")]
        [InlineData("RepeatedGroup2", "Custom")]
        public void AddGroup_AlreadyExists_Conflict(string groupName, string groupSource)
        {
            Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = groupName,
                    GroupSource = groupSource
                });
            }).Wait();

            // Repeat
            var postResponse = Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = groupName,
                    GroupSource = groupSource
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Conflict, postResponse.StatusCode);
        }

        [Theory, IntegrationTestsFixture.DisplayTestMethodName,
         InlineData("BatchUpdateGroup1", "BatchUpdateSource1"), InlineData("BatchUpdateGroup2", "BatchUpdateSource2")]
        public void UpdateGroup_Batch_Success(string groupName, string groupSource)
        {
            groupName = groupName + Guid.NewGuid();
            var postResponse = Browser.Post("/groups/UpdateGroups", with =>
            {
                with.HttpRequest();
               with.JsonBody(new []
                {
                    new
                    {
                        Id = groupName + "_0",
                        GroupName = groupName + "_0",
                        GroupSource = groupSource + "_0"
                    },
                    new
                    {
                        Id = groupName + "_1",
                        GroupName = groupName + "_1",
                        GroupSource = groupSource + "_1"
                    },
                    new
                    {
                        Id = groupName + "_2",
                        GroupName = groupName + "_2",
                        GroupSource = groupSource + "_2"
                    }
                });

                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.NoContent, postResponse.StatusCode);

            // Replace groups. _0 should be removed and _3 should be added.
            postResponse = Browser.Post("/groups/UpdateGroups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new
                    {
                        Id = groupName + "_1",
                        GroupName = groupName + "_1",
                        GroupSource = groupSource + "_1"
                    },
                    new
                    {
                        Id = groupName + "_2",
                        GroupName = groupName + "_2",
                        GroupSource = groupSource + "_2"
                    },
                    new
                    {
                        Id = groupName + "_3",
                        GroupName = groupName + "_3",
                        GroupSource = groupSource + "_3"
                    }
                });
            }).Result;

            Assert.Equal(HttpStatusCode.NoContent, postResponse.StatusCode);

            var getResponse0 = Browser.Get($"/groups/{groupName}_0", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            var getResponse1 = Browser.Get($"/groups/{groupName}_1", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            var getResponse2 = Browser.Get($"/groups/{groupName}_2", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            var getResponse3 = Browser.Get($"/groups/{groupName}_3", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.NotFound, getResponse0.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse1.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse2.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse3.StatusCode);

            Assert.Contains(groupName + "_1", getResponse1.Body.AsString());
            Assert.Contains(groupName + "_2", getResponse2.Body.AsString());
            Assert.Contains(groupName + "_3", getResponse3.Body.AsString());
        }

        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodNameAttribute]
        [InlineData("GroupToBeDeleted", "Source1")]
        [InlineData("GroupToBeDeleted2", "Source2")]
        public void DeleteGroup_SingleGroup_Success(string groupName, string groupSource)
        {
            Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = groupName,
                    GroupSource = groupSource
                });
            }).Wait();

            var delete = Browser.Delete($"/groups/{groupName}", with =>
            {
                with.HttpRequest();
            }).Result;

            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        }

        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodNameAttribute]
        [InlineData("NonexistentGroup")]
        [InlineData("NonexistentGroup2")]
        public void DeleteGroup_NonExistentGroup_NotFound(string groupName)
        {
            var delete = Browser.Delete($"/groups/{groupName}", with =>
            {
                with.HttpRequest();
            }).Result;

            Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        }

        #region Role->Group Mapping Tests

        protected void SetupGroup(string groupName, string groupSource)
        {
            var response = Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = groupName,
                    GroupSource = groupSource
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        protected Guid SetupRole(string roleName)
        {
            var response = Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = "rolesprincipal",
                    Name = roleName
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var id = response.Body.DeserializeJson<RoleApiModel>().Id;

            if (id == null)
                throw new Exception("Guid not generated.");

            return id.Value;
        }

        protected BrowserResponse SetupGroupRoleMapping(string groupName, string roleId)
        {
            var response = Browser.Post($"/groups/{groupName}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Id = roleId
                });
            }).Result;

            return response;
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public void AddRoleToGroup_GroupExists_Success()
        {
            string group1Name = "Group1Name" + Guid.NewGuid();
            string role1Name = "Role1Name" + Guid.NewGuid();
            SetupGroup(group1Name, "Custom");
            var roleId = SetupRole(role1Name);
            var response = SetupGroupRoleMapping(group1Name, roleId.ToString());

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            response = Browser.Get($"/groups/{group1Name}/roles", with =>
            {
                with.HttpRequest();
            }).Result;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseEntity = response.Body.DeserializeJson<GroupRoleApiModel>();
            var roleList = responseEntity.Roles.ToList();
            Assert.Single(roleList);
            Assert.Equal(role1Name, roleList[0].Name);

            // set up another role->group mapping
            string group2Name = "Group2Name" + Guid.NewGuid();
            string role2Name = "Role2Name" + Guid.NewGuid();
            SetupGroup(group2Name, "Custom");
            roleId = SetupRole(role2Name);
            response = SetupGroupRoleMapping(group2Name, roleId.ToString());

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            response = Browser.Get($"/groups/{group2Name}/roles", with =>
            {
                with.HttpRequest();
            }).Result;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            responseEntity = response.Body.DeserializeJson<GroupRoleApiModel>();
            roleList = responseEntity.Roles.ToList();
            Assert.Single(roleList);
            Assert.Equal(role2Name, roleList[0].Name);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public void AddRoleToGroup_NonExistentGroup_NotFound()
        {
            var roleId = SetupRole("RoleName");
            var response = SetupGroupRoleMapping("NonexistentGroup", roleId.ToString());
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact, IntegrationTestsFixture.DisplayTestMethodName]
        public void AddRoleToGroup_GroupRoleMappingAlreadyExists_AlreadyExistsException()
        {
            string group1Name = "Group1Name" + Guid.NewGuid();            
            SetupGroup(group1Name, "Custom");
            string role1Name = "Role1Name" + Guid.NewGuid();
            var roleId = SetupRole(role1Name);
            var response = SetupGroupRoleMapping(group1Name, roleId.ToString());

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // attempt to set up the same mapping (the API treats this as an update to the existing
            // group-role mapping)
            response = SetupGroupRoleMapping(group1Name, roleId.ToString());
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Contains($"Role {role1Name} already exists for group {group1Name}", response.Body.AsString());
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public void DeleteRoleFromGroup_GroupExists_Success()
        {
            string group1Name = "Group1Name" + Guid.NewGuid();
            SetupGroup(group1Name, "Custom");
            string role1Name = "Role1Name" + Guid.NewGuid();
            var roleId = SetupRole(role1Name);
            var response = SetupGroupRoleMapping(group1Name, roleId.ToString());

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // delete the mapping
            response = Browser.Delete($"/groups/{group1Name}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Id = roleId.ToString()
                });
            }).Result;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            response = Browser.Get($"/groups/{group1Name}/roles", with =>
            {
                with.HttpRequest();
            }).Result;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseEntity = response.Body.DeserializeJson<GroupRoleApiModel>();
            var roleList = responseEntity.Roles.ToList();
            Assert.Empty(roleList);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public void DeleteRoleFromGroup_NonExistentGroup_NotFound()
        {            
            var roleId = SetupRole("RoleName" + Guid.NewGuid());
            var response = Browser.Delete("/groups/invalidGroup/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Id = roleId.ToString()
                });
            }).Result;

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public void DeleteRoleFromGroup_NonExistentGroupRoleMapping_NotFound()
        {
            SetupGroup("Group1Name", "Custom");
            var response = Browser.Delete("/groups/Group1Name/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Id = Guid.NewGuid().ToString()
                });
            }).Result;

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public void GetRolesForGroup_NonExistentGroup_NotFound()
        {
            var response = Browser.Get("/groups/invalidGroup/roles", with =>
            {
                with.HttpRequest();
            }).Result;

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        #endregion

        #region User->Group Mapping Tests 

        protected BrowserResponse SetupGroupUserMapping(string groupName, string subjectId, string identityProvider)
        {
            var response = Browser.Post($"/groups/{groupName}/users", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.JsonBody(new
                {
                    SubjectId = subjectId,
                    IdentityProvider = identityProvider
                });
            }).Result;

            return response;
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public void AddUserToGroup_GroupExists_Success()
        {
            string group1Name = "Group1Name" + Guid.NewGuid();
            const string user1SubjectId = "User1SubjectId";
            const string identityProvider = "idP1";

            SetupGroup(group1Name, "Custom");
            var response = SetupGroupUserMapping(group1Name, user1SubjectId, identityProvider);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            response = Browser.Get($"/groups/{group1Name}/users", with =>
            {
                with.HttpRequest();
            }).Result;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseEntity = response.Body.DeserializeJson<GroupUserApiModel>();
            var userList = responseEntity.Users.ToList();
            Assert.Single(userList);
            Assert.Equal(user1SubjectId, userList[0].SubjectId);

            // set up another user->group mapping
            string group2Name = "Group2Name" + Guid.NewGuid();
            const string user2SubjectId = "User2SubjectId";

            SetupGroup(group2Name, "Custom");

            // link user 2 to group 1
            response = SetupGroupUserMapping(group1Name, user2SubjectId, identityProvider);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // link user 2 to group 2
            response = SetupGroupUserMapping(group2Name, user2SubjectId, identityProvider);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // get users for group 1
            response = Browser.Get($"/groups/{group1Name}/users", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            responseEntity = response.Body.DeserializeJson<GroupUserApiModel>();
            userList = responseEntity.Users.ToList();
            Assert.Equal(2, userList.Count);

            // get users for group 2
            response = Browser.Get($"/groups/{group2Name}/users", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            responseEntity = response.Body.DeserializeJson<GroupUserApiModel>();
            userList = responseEntity.Users.ToList();
            Assert.Single(userList);
            Assert.Equal(user2SubjectId, userList[0].SubjectId);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodNameAttribute]
        public void AddUserToGroup_NonCustomGroup_BadRequest()
        {
            string group1Name = "Group1Name" + Guid.NewGuid();
            const string user1SubjectId = "User1SubjectId";

            SetupGroup(group1Name, "Active Directory");
            var response = SetupGroupUserMapping(group1Name, user1SubjectId, "idP1");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public void AddUserToGroup_NoSubjectId_BadRequest()
        {
            string group1Name = "Group1Name" + Guid.NewGuid();
            const string identityProvider = "idP1";

            SetupGroup(group1Name, "Custom");
            var response = SetupGroupUserMapping(group1Name, null, identityProvider);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public void AddUserToGroup_NoIdentityProvider_BadRequest()
        {
            string group1Name = "Group1Name" + Guid.NewGuid();
            const string user1SubjectId = "User1SubjectId";

            SetupGroup(group1Name, "Custom");
            var response = SetupGroupUserMapping(group1Name, user1SubjectId, "");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public void AddUserToGroup_NonExistentGroup_NotFound()
        {
            var response = SetupGroupUserMapping("NonexistentGroup", "SubjectId", "idP1");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public void AddUserToGroup_GroupUserMappingAlreadyExists_Success()
        {
            string group1Name = "Group1Name" + Guid.NewGuid();
            const string subject1Id = "Subject1Id";
            const string identityProvider = "idP1";

            SetupGroup(group1Name, "Custom");
            var response = SetupGroupUserMapping(group1Name, subject1Id, identityProvider);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // attempt to set up the same mapping (the API treats this as an update to the existing
            // group-user mapping)
            response = SetupGroupUserMapping(group1Name, subject1Id, identityProvider);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            response = Browser.Get($"/groups/{group1Name}/users", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseEntity = response.Body.DeserializeJson<GroupUserApiModel>();
            var userList = responseEntity.Users.ToList();
            Assert.Single(userList);
            Assert.Equal(subject1Id, userList[0].SubjectId);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public void DeleteUserFromGroup_GroupExists_Success()
        {
            string group1Name = "Group1Name" + Guid.NewGuid();
            SetupGroup(group1Name, "Custom");
            const string subject1Id = "Subject1Id";
            const string identityProvider = "idP1";
            var response = SetupGroupUserMapping(group1Name, subject1Id, identityProvider);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // delete the mapping
            response = Browser.Delete($"/groups/{group1Name}/users", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    SubjectId = subject1Id,
                    IdentityProvider = identityProvider
                });

            }).Result;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            response = Browser.Get($"/groups/{group1Name}/users", with =>
            {
                with.HttpRequest();
            }).Result;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseEntity = response.Body.DeserializeJson<GroupUserApiModel>();
            var userList = responseEntity.Users.ToList();
            Assert.Empty(userList);

            // ensure the deletion is reflected in the user model
            response = Browser.Get($"/user/{identityProvider}/{subject1Id}/groups", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var groups = response.Body.DeserializeJson<string[]>();
            Assert.Empty(groups);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public void DeleteUserFromGroup_NonExistentGroup_NotFound()
        {
            var response = Browser.Delete("/groups/invalidGroup/users", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    SubjectId = "SubjectId",
                    IdentityProvider = "idP1"
                });
            }).Result;

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public void DeleteUserFromGroup_NonExistentGroupUserMapping_NotFound()
        {
            string group1Name = "Group1Name" + Guid.NewGuid();

            SetupGroup(group1Name, "Custom");
            var response = Browser.Delete($"/groups/{group1Name}/users", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    SubjectId = "Subject1Id" + Guid.NewGuid(),
                    IdentityProvider = "idP1"
                });
            }).Result;

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public void DeleteUserFromGroup_NoSubjectId_BadRequest()
        {
            string group1Name = "Group1Name" + Guid.NewGuid();
            SetupGroup(group1Name, "Custom");
            string subject1Id = "Subject1Id" + Guid.NewGuid();
            string identityProvider = "idP1" + Guid.NewGuid();
            var response = SetupGroupUserMapping(group1Name, subject1Id, identityProvider);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // attempt to delete the mapping
            response = Browser.Delete($"/groups/{group1Name}/users", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    IdentityProvider = identityProvider
                });
            }).Result;

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public void DeleteUserFromGroup_NoIdentityProvider_BadRequest()
        {
            string group1Name = "Group1Name" + Guid.NewGuid();
            SetupGroup(group1Name, "Custom");
            string subject1Id = "Subject1Id" + Guid.NewGuid();
            string identityProvider = "idP1" + Guid.NewGuid();
            var response = SetupGroupUserMapping(group1Name, subject1Id, identityProvider);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // attempt to delete the mapping
            response = Browser.Delete($"/groups/{group1Name}/users", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    SubjectId = subject1Id,
                });
            }).Result;

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public void GetUsersForGroup_NonExistentGroup_NotFound()
        {
            var response = Browser.Get("/groups/invalidGroup/users", with =>
            {
                with.HttpRequest();
            }).Result;

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public void GetGroupsForUser_GroupAndUserExist_Success()
        {
            string groupName = "GroupName" + Guid.NewGuid();
            string subjectId = "Subject1Id" + Guid.NewGuid();
            string identityProvider = "idP1" + Guid.NewGuid();
            SetupGroup(groupName, "Custom");
            SetupGroupUserMapping(groupName, subjectId, identityProvider);

            var response = Browser.Get($"/user/{identityProvider}/{subjectId}/groups", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var groupList = response.Body.DeserializeJson<string[]>();
            Assert.Single(groupList);
            Assert.Equal(groupName, groupList[0]);
        }

        #endregion

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public void GetGroups_AddPermissionToRole_AllGroupsSynced()
        {
            string groupName = "Admin" + Guid.NewGuid();
            string roleName = "Administrator" + Guid.NewGuid();
            string permissionName = "app-write" + Guid.NewGuid();

            // create group
            var postResponse = Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = groupName,
                    GroupSource = "Custom"
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            // create role
            postResponse = Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = "rolesprincipal",
                    Name = roleName
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            var role = postResponse.Body.DeserializeJson<RoleApiModel>();
            var roleId = role.Id.ToString();

            // add role to group
            postResponse = Browser.Post($"/groups/{groupName}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Id = roleId
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            // add permission
            postResponse = Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = "rolesprincipal",
                    Name = permissionName
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            var permission = postResponse.Body.DeserializeJson<PermissionApiModel>();

            var permissionApiModels = new List<PermissionApiModel>
            {
                new PermissionApiModel
                {
                    Id = permission.Id,
                    Grain = "app",
                    SecurableItem = "rolesprincipal",
                    Name = permissionName
                }
            };

            // add permission to role
            postResponse = Browser.Post($"/roles/{roleId}/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(permissionApiModels);
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            VerifyPermission(groupName, roleName, permissionName, true);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public void GetGroups_DeletePermissionFromRole_AllGroupsSynced()
        {
            string groupName = "Admin" + Guid.NewGuid();
            const string roleName = "Administrator";
            const string permissionName = "app-write";

            // create group
            var postResponse = Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = groupName,
                    GroupSource = "Custom"
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            // create role
            postResponse = Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = "rolesprincipal",
                    Name = roleName
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            var role = postResponse.Body.DeserializeJson<RoleApiModel>();
            var roleId = role.Id.ToString();

            // add role to group
            postResponse = Browser.Post($"/groups/{groupName}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Id = roleId
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            // add permission
            postResponse = Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = "rolesprincipal",
                    Name = permissionName
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            var permission = postResponse.Body.DeserializeJson<PermissionApiModel>();

            var permissionApiModels = new List<PermissionApiModel>
            {
                new PermissionApiModel
                {
                    Id = permission.Id,
                    Grain = "app",
                    SecurableItem = "rolesprincipal",
                    Name = permissionName
                }
            };

            // add permission to role
            postResponse = Browser.Post($"/roles/{roleId}/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(permissionApiModels);
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            VerifyPermission(groupName, roleName, permissionName, true);

            // delete permission from role
            postResponse = Browser.Delete($"/roles/{roleId}/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(permissionApiModels);
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            VerifyPermission(groupName, roleName, permissionName, false);
        }

        // ReSharper disable once UnusedParameter.Local
        private void VerifyPermission(string groupName, string roleName, string permissionName, bool exists)
        {
            // get the group
            var getResponse = Browser.Get($"/groups/{groupName}", with =>
            {
                with.HttpRequest();
            }).Result;

            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var group = getResponse.Body.DeserializeJson<GroupRoleApiModel>();
            var adminRole = group.Roles.FirstOrDefault(r => r.Name == roleName);
            Assert.NotNull(adminRole);

            var appWritePermission = adminRole.Permissions.FirstOrDefault(p => p.Name == permissionName);
            if (exists)
            {
                Assert.NotNull(appWritePermission);
            }
            else
            {
                Assert.Null(appWritePermission);
            }
        }
    }
}