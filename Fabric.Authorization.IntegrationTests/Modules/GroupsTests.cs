using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Fabric.Authorization.API;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.API.Modules;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Domain.Stores.CouchDB;
using Fabric.Authorization.Domain.Stores.InMemory;
using Fabric.Authorization.Domain.Stores.Services;
using Fabric.Authorization.Domain.Validators;
using Nancy;
using Nancy.Testing;
using Xunit;

namespace Fabric.Authorization.IntegrationTests.Modules
{
    [Collection("InMemoryTests")]
    public class GroupsTests : IntegrationTestsFixture
    {
        public GroupsTests(bool useInMemoryDB = true)
        {
            var groupStore = useInMemoryDB
                ? new InMemoryGroupStore()
                : (IGroupStore) new CouchDbGroupStore(DbService(), Logger, EventContextResolverService);

            var roleStore = useInMemoryDB
                ? new InMemoryRoleStore()
                : (IRoleStore) new CouchDbRoleStore(DbService(), Logger, EventContextResolverService);

            var userStore = useInMemoryDB
                ? new InMemoryUserStore()
                : (IUserStore)new CouchDbUserStore(DbService(), Logger, EventContextResolverService);

            var permissionStore = useInMemoryDB
                ? new InMemoryPermissionStore()
                : (IPermissionStore)new CouchDbPermissionStore(DbService(), Logger, EventContextResolverService);

            var clientStore = useInMemoryDB
                ? new InMemoryClientStore()
                : (IClientStore)new CouchDbClientStore(DbService(), Logger, EventContextResolverService);

            var groupService = new GroupService(groupStore, roleStore, userStore);
            var userService = new UserService(userStore);
            var roleService = new RoleService(roleStore, new InMemoryPermissionStore());
            var clientService = new ClientService(clientStore);
            var permissionService = new PermissionService(permissionStore, roleService);

            Browser = new Browser(with =>
            {
                with.Module(new GroupsModule(
                    groupService,
                    new GroupValidator(groupService),
                    Logger));

                with.Module(new RolesModule(
                    roleService,
                    clientService,
                    new RoleValidator(roleService),
                    Logger));

                with.Module(new ClientsModule(
                    clientService,
                    new ClientValidator(clientService),
                    Logger));

                with.Module(new UsersModule(
                    clientService,
                    permissionService,
                    userService,
                    new UserValidator(),
                    Logger));

                with.RequestStartup((_, pipelines, context) =>
                {
                    context.CurrentUser = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                    {
                        new Claim(Claims.Scope, Scopes.ManageClientsScope),
                        new Claim(Claims.Scope, Scopes.ReadScope),
                        new Claim(Claims.Scope, Scopes.WriteScope),
                        new Claim(Claims.ClientId, "rolesprincipal")
                    }, "rolesprincipal"));
                    pipelines.BeforeRequest += ctx => RequestHooks.SetDefaultVersionInUrl(ctx);
                });
            }, withDefaults => withDefaults.HostName("testhost"));

            Browser.Post("/clients", with =>
            {
                with.HttpRequest();
                with.FormValue("Id", "rolesprincipal");
                with.FormValue("Name", "rolesprincipal");
                with.Header("Accept", "application/json");
            }).Wait();
        }

        [Theory]
        [DisplayTestMethodName]
        [InlineData("NonexistentGroup")]
        [InlineData("NonexistentGroup2")]
        public void GetGroup_NonexistentGroup_Fail(string groupName)
        {
            var get = Browser.Get($"/groups/{groupName}", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        }

        [Theory]
        [DisplayTestMethodName]
        [InlineData("Group1", "Source1")]
        [InlineData("Group2", "Source2")]
        [InlineData("6BC32347-36A1-44CF-AA0E-6C1038AA1DF3", "Source3")]
        public void AddGroup_SingleGroup_Success(string groupName, string groupSource)
        {
            var postResponse = Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("Id", groupName);
                with.FormValue("GroupName", groupName);
                with.FormValue("GroupSource", groupSource);
                with.Header("Accept", "application/json");
            }).Result;

            var getResponse = Browser.Get($"/groups/{groupName}", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.True(getResponse.Body.AsString().Contains(groupName));
        }

        [Theory]
        [DisplayTestMethodName]
        [InlineData("BatchGroup1", "BatchSource1")]
        [InlineData("BatchGroup2", "BatchSource2")]
        [InlineData("6AC32A47-36C1-23BF-AA22-6C1028AA5DC3", "BatchSource3")]
        public void AddGroup_Batch_Success(string groupName, string groupSource)
        {
            var postResponse = Browser.Post("/groups/UpdateGroups", with =>
            {
                with.HttpRequest();
                with.FormValue("Id[0]", groupName + "_0");
                with.FormValue("Id[1]", groupName + "_1");
                with.FormValue("Id[2]", groupName + "_2");

                with.FormValue("GroupName[0]", groupName + "_0");
                with.FormValue("GroupName[1]", groupName + "_1");
                with.FormValue("GroupName[2]", groupName + "_2");

                with.FormValue("GroupSource[0]", groupSource + "_0");
                with.FormValue("GroupSource[1]", groupSource + "_1");
                with.FormValue("GroupSource[2]", groupSource + "_2");

                with.Header("Accept", "application/json");
            }).Result;

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

            Assert.Equal(HttpStatusCode.NoContent, postResponse.StatusCode);

            Assert.Equal(HttpStatusCode.OK, getResponse0.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse1.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse2.StatusCode);

            Assert.True(getResponse0.Body.AsString().Contains(groupName + "_0"));
            Assert.True(getResponse1.Body.AsString().Contains(groupName + "_1"));
            Assert.True(getResponse2.Body.AsString().Contains(groupName + "_2"));

            Assert.True(getResponse0.Body.AsString().Contains(groupSource + "_0"));
            Assert.True(getResponse1.Body.AsString().Contains(groupSource + "_1"));
            Assert.True(getResponse2.Body.AsString().Contains(groupSource + "_2"));
        }

        [Theory]
        [DisplayTestMethodName]
        [InlineData("", "Source1")]
        [InlineData(null, "Source2")]
        public void AddGroup_NullOrEmptyName_BadRequest(string groupName, string groupSource)
        {
            var postResponse = Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("Id", groupName);
                with.FormValue("GroupName", groupName);
                with.FormValue("GroupSource", groupSource);
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.BadRequest, postResponse.StatusCode);
        }

        [Theory]
        [DisplayTestMethodName]
        [InlineData("Source1")]
        public void AddGroup_MissingName_BadRequest(string groupSource)
        {
            var postResponse = Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("Id", "GroupId");
                with.FormValue("GroupSource", groupSource);
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.BadRequest, postResponse.StatusCode);
        }

        [Theory]
        [DisplayTestMethodName]
        [InlineData("Source1", "")]
        [InlineData("Source2", null)]
        public void AddGroup_NullOrEmptySource_BadRequest(string groupName, string groupSource)
        {
            var postResponse = Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("Id", groupName);
                with.FormValue("GroupName", groupName);
                with.FormValue("GroupSource", groupSource);
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.BadRequest, postResponse.StatusCode);
        }

        [Theory]
        [DisplayTestMethodName]
        [InlineData("Name1")]
        public void AddGroup_MissingSource_BadRequest(string groupName)
        {
            var postResponse = Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("Id", groupName);
                with.FormValue("GroupName", groupName);
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.BadRequest, postResponse.StatusCode);
        }

        [Theory]
        [DisplayTestMethodName]
        [InlineData("RepeatedGroup1")]
        [InlineData("RepeatedGroup2")]
        public void AddGroup_AlreadyExists_Fail(string groupName)
        {
            Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("Id", groupName);
                with.FormValue("GroupName", groupName);
                with.Header("Accept", "application/json");
            }).Wait();

            // Repeat
            var postResponse = Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("Id", groupName);
                with.FormValue("GroupName", groupName);
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.BadRequest, postResponse.StatusCode);
        }

        [Theory]
        [DisplayTestMethodName]
        [InlineData("BatchUpdateGroup1", "BatchUpdateSource1")]
        [InlineData("BatchUpdateGroup2", "BatchUpdateSource2")]
        public void UpdateGroup_Batch_Success(string groupName, string groupSource)
        {
            var postResponse = Browser.Post("/groups/UpdateGroups", with =>
            {
                with.HttpRequest();
                with.FormValue("Id[0]", groupName + "_0");
                with.FormValue("Id[1]", groupName + "_1");
                with.FormValue("Id[2]", groupName + "_2");

                with.FormValue("GroupName[0]", groupName + "_0");
                with.FormValue("GroupName[1]", groupName + "_1");
                with.FormValue("GroupName[2]", groupName + "_2");

                with.FormValue("GroupSource[0]", groupSource + "_0");
                with.FormValue("GroupSource[1]", groupSource + "_1");
                with.FormValue("GroupSource[2]", groupSource + "_2");

                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.NoContent, postResponse.StatusCode);

            // Replace groups. _0 should be removed and _3 should be added.
            postResponse = Browser.Post("/groups/UpdateGroups", with =>
            {
                with.HttpRequest();
                with.FormValue("Id[0]", groupName + "_1");
                with.FormValue("Id[1]", groupName + "_2");
                with.FormValue("Id[2]", groupName + "_3");
                with.FormValue("GroupName[0]", groupName + "_1");
                with.FormValue("GroupName[1]", groupName + "_2");
                with.FormValue("GroupName[2]", groupName + "_3");
                with.Header("Accept", "application/json");
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

            Assert.True(getResponse1.Body.AsString().Contains(groupName + "_1"));
            Assert.True(getResponse2.Body.AsString().Contains(groupName + "_2"));
            Assert.True(getResponse3.Body.AsString().Contains(groupName + "_3"));
        }

        [Theory]
        [DisplayTestMethodName]
        [InlineData("GroupToBeDeleted", "Source1")]
        [InlineData("GroupToBeDeleted2", "Source2")]
        public void DeleteGroup_SingleGroup_Success(string groupName, string groupSource)
        {
            Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("Id", groupName);
                with.FormValue("GroupName", groupName);
                with.FormValue("GroupSource", groupSource);
                with.Header("Accept", "application/json");
            }).Wait();

            var delete = Browser.Delete($"/groups/{groupName}", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        }

        [Theory]
        [DisplayTestMethodName]
        [InlineData("NonexistentGroup")]
        [InlineData("NonexistentGroup2")]
        public void DeleteGroup_NonExistentGroup_Fail(string groupName)
        {
            var delete = Browser.Delete($"/groups/{groupName}", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        }

        #region Role->Group Mapping Tests

        private void SetupGroup(string groupName, string groupSource)
        {
            var response = Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("Id", groupName);
                with.FormValue("GroupName", groupName);
                with.FormValue("GroupSource", groupSource);
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        private Guid? SetupRole(string roleName)
        {
            var response = Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "rolesprincipal");
                with.FormValue("Name", roleName);
            }).Result;

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            return response.Body.DeserializeJson<RoleApiModel>().Id;
        }

        private BrowserResponse SetupGroupRoleMapping(string groupName, string roleId)
        {
            var response = Browser.Post($"/groups/{groupName}/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Id", roleId);
            }).Result;

            return response;
        }

        [Fact]
        [DisplayTestMethodName]
        public void AddRoleToGroup_GroupExists_Success()
        {
            const string group1Name = "Group1Name";
            SetupGroup(group1Name, "Custom");
            var roleId = SetupRole("Role1Name");
            var response = SetupGroupRoleMapping(group1Name, roleId.ToString());

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            response = Browser.Get($"/groups/{group1Name}/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseEntity = response.Body.DeserializeJson<GroupRoleApiModel>();
            var roleList = responseEntity.Roles.ToList();
            Assert.Equal(1, roleList.Count);
            Assert.Equal("Role1Name", roleList[0].Name);

            // set up another role->group mapping
            const string group2Name = "Group2Name";
            SetupGroup(group2Name, "Custom");
            roleId = SetupRole("Role2Name");
            response = SetupGroupRoleMapping(group2Name, roleId.ToString());

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            response = Browser.Get($"/groups/{group2Name}/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            responseEntity = response.Body.DeserializeJson<GroupRoleApiModel>();
            roleList = responseEntity.Roles.ToList();
            Assert.Equal(1, roleList.Count);
            Assert.Equal("Role2Name", roleList[0].Name);
        }

        [Fact]
        [DisplayTestMethodName]
        public void AddRoleToGroup_NonExistentGroup_Fail()
        {
            var roleId = SetupRole("RoleName");
            var response = SetupGroupRoleMapping("NonexistentGroup", roleId.ToString());
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        [DisplayTestMethodName]
        public void AddRoleToGroup_GroupRoleMappingAlreadyExists_Success()
        {
            const string group1Name = "Group1Name";
            SetupGroup(group1Name, "Custom");
            var roleId = SetupRole("Role1Name");
            var response = SetupGroupRoleMapping(group1Name, roleId.ToString());

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // attempt to set up the same mapping (the API treats this as an update to the existing
            // group-role mapping)
            response = SetupGroupRoleMapping(group1Name, roleId.ToString());
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            response = Browser.Get($"/groups/{group1Name}/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseEntity = response.Body.DeserializeJson<GroupRoleApiModel>();
            var roleList = responseEntity.Roles.ToList();
            Assert.Equal(1, roleList.Count);
            Assert.Equal("Role1Name", roleList[0].Name);
        }

        [Fact(Skip = "Test does not pass when run against CouchDB")]
        [DisplayTestMethodName]
        public void DeleteRoleFromGroup_GroupExists_Success()
        {
            const string group1Name = "Group1Name";
            SetupGroup(group1Name, "Custom");
            var roleId = SetupRole("Role1Name");
            var response = SetupGroupRoleMapping(group1Name, roleId.ToString());

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // delete the mapping
            response = Browser.Delete($"/groups/{group1Name}/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Id", roleId.ToString());
            }).Result;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            response = Browser.Get($"/groups/{group1Name}/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseEntity = response.Body.DeserializeJson<GroupRoleApiModel>();
            var roleList = responseEntity.Roles.ToList();
            Assert.Equal(0, roleList.Count);
        }

        [Fact]
        [DisplayTestMethodName]
        public void DeleteRoleFromGroup_NonExistentGroup_Fail()
        {
            var roleId = SetupRole("RoleName");
            var response = Browser.Delete("/groups/invalidGroup/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Id", roleId.ToString());
            }).Result;

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        [DisplayTestMethodName]
        public void DeleteRoleFromGroup_NonExistentGroupRoleMapping_Fail()
        {
            SetupGroup("Group1Name", "Custom");
            var response = Browser.Delete("/groups/Group1Name/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Id", "invalidRole");
            }).Result;

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        [DisplayTestMethodName]
        public void GetRolesForGroup_NonExistentGroup_Success()
        {
            var response = Browser.Get("/groups/invalidGroup/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseEntity = response.Body.DeserializeJson<GroupRoleApiModel>();
            var roleList = responseEntity.Roles.ToList();
            Assert.Equal(0, roleList.Count);
        }

        #endregion

        #region User->Group Mapping Tests 

        private BrowserResponse SetupGroupUserMapping(string groupName, string subjectId)
        {
            var response = Browser.Post($"/groups/{groupName}/users", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("SubjectId", subjectId);
            }).Result;

            return response;
        }

        [Fact]
        [DisplayTestMethodName]
        public void AddUserToGroup_GroupExists_Success()
        {
            const string group1Name = "Group1Name";
            const string user1SubjectId = "User1SubjectId";

            SetupGroup(group1Name, "Custom");
            var response = SetupGroupUserMapping(group1Name, user1SubjectId);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            response = Browser.Get($"/groups/{group1Name}/users", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseEntity = response.Body.DeserializeJson<GroupUserApiModel>();
            var userList = responseEntity.Users.ToList();
            Assert.Equal(1, userList.Count);
            Assert.Equal(user1SubjectId, userList[0].SubjectId);

            // set up another role->group mapping
            const string group2Name = "Group2Name";
            const string user2SubjectId = "User2SubjectId";

            SetupGroup(group2Name, "Custom");
            response = SetupGroupUserMapping(group2Name, user2SubjectId);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            response = Browser.Get($"/groups/{group2Name}/users", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            responseEntity = response.Body.DeserializeJson<GroupUserApiModel>();
            userList = responseEntity.Users.ToList();
            Assert.Equal(1, userList.Count);
            Assert.Equal(user2SubjectId, userList[0].SubjectId);
        }

        [Fact]
        [DisplayTestMethodName]
        public void AddUserToGroup_NonExistentGroup_Fail()
        {
            var response = SetupGroupUserMapping("NonexistentGroup", "SubjectId");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        [DisplayTestMethodName]
        public void AddUserToGroup_GroupUserMappingAlreadyExists_Success()
        {
            const string group1Name = "Group1Name";
            const string subject1Id = "Subject1Id";

            SetupGroup(group1Name, "Custom");
            var response = SetupGroupUserMapping(group1Name, subject1Id);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // attempt to set up the same mapping (the API treats this as an update to the existing
            // group-user mapping)
            response = SetupGroupUserMapping(group1Name, subject1Id);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            response = Browser.Get($"/groups/{group1Name}/users", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseEntity = response.Body.DeserializeJson<GroupUserApiModel>();
            var userList = responseEntity.Users.ToList();
            Assert.Equal(1, userList.Count);
            Assert.Equal(subject1Id, userList[0].SubjectId);
        }

        [Fact(Skip = "Test does not pass when run against CouchDB")]
        [DisplayTestMethodName]
        public void DeleteUserFromGroup_GroupExists_Success()
        {
            const string group1Name = "Group1Name";
            SetupGroup(group1Name, "Custom");
            const string subject1Id = "Subject1Id";
            var response = SetupGroupUserMapping(group1Name, subject1Id);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // delete the mapping
            response = Browser.Delete($"/groups/{group1Name}/users", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("SubjectId", subject1Id);
            }).Result;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            response = Browser.Get($"/groups/{group1Name}/users", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseEntity = response.Body.DeserializeJson<GroupUserApiModel>();
            var userList = responseEntity.Users.ToList();
            Assert.Equal(0, userList.Count);
        }

        [Fact]
        [DisplayTestMethodName]
        public void DeleteUserFromGroup_NonExistentGroup_Fail()
        {
            var response = Browser.Delete("/groups/invalidGroup/users", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("SubjectId", "SubjectId");
            }).Result;

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        [DisplayTestMethodName]
        public void DeleteUserFromGroup_NonExistentGroupUserMapping_Fail()
        {
            SetupGroup("Group1Name", "Custom");
            var response = Browser.Delete("/groups/Group1Name/users", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("SubjectId", "Subject1Id");
            }).Result;

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        [DisplayTestMethodName]
        public void GetUsersForGroup_NonExistentGroup_Fail()
        {
            var response = Browser.Get("/groups/invalidGroup/users", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseEntity = response.Body.DeserializeJson<GroupUserApiModel>();
            var userList = responseEntity.Users.ToList();
            Assert.Equal(0, userList.Count);
        }

        [Fact]
        [DisplayTestMethodName]
        public void GetGroupsForUser_GroupAndUserExist_Success()
        {
            const string groupName = "GroupName";
            SetupGroup(groupName, "Custom");
            SetupGroupUserMapping(groupName, "Subject1Name");

            var response = Browser.Get("/users/Subject1Name/groups", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var groupList = response.Body.DeserializeJson<string[]>();
            Assert.Equal(1, groupList.Length);
            Assert.Equal(groupName, groupList[0]);
        }

        #endregion
    }
}