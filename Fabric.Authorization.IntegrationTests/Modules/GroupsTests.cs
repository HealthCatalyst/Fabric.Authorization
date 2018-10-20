using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Catalyst.Fabric.Authorization.Models;
using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Persistence.SqlServer.Configuration;
using Nancy;
using Nancy.Testing;
using Newtonsoft.Json;
using Xunit;

namespace Fabric.Authorization.IntegrationTests.Modules
{
    [Collection("InMemoryTests")]
    public class GroupsTests : IClassFixture<IntegrationTestsFixture>
    {
        protected readonly Browser Browser;
        private readonly DefaultPropertySettings _defaultPropertySettings;
        private readonly IntegrationTestsFixture _fixture;
        private readonly string _storageProvider;

        protected ClaimsPrincipal Principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
        {
            new Claim(Claims.Scope, Scopes.ManageClientsScope),
            new Claim(Claims.Scope, Scopes.ReadScope),
            new Claim(Claims.Scope, Scopes.WriteScope),
            new Claim(Claims.ClientId, "rolesprincipal"),
            new Claim(Claims.IdentityProvider, "idP1")
        }, "rolesprincipal"));

        public GroupsTests(IntegrationTestsFixture fixture, string storageProvider = StorageProviders.InMemory, ConnectionStrings connectionStrings = null)
        {
            if (connectionStrings != null)
            {
                fixture.ConnectionStrings = connectionStrings;
            }
            Browser = fixture.GetBrowser(Principal, storageProvider);
            _defaultPropertySettings = fixture.DefaultPropertySettings;
            fixture.CreateClient(Browser, "rolesprincipal");
            _fixture = fixture;
            _storageProvider = storageProvider;
        }

        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodName]
        [InlineData("NonexistentGroup")]
        [InlineData("NonexistentGroup2")]
        public async Task GetGroup_NonexistentGroup_NotFoundAsync(string groupName)
        {
            var get = await Browser.Get($"/groups/{groupName}", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        }

        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodName]
        [InlineData("AddGroup_SingleGroup_SuccessAsync1", "Source1", "DisplayName1", "Description1", "Windows")]
        [InlineData("AddGroup_SingleGroup_SuccessAsync2", "Source2", "DisplayName2", "Description2", "Windows")]
        [InlineData("6BC32347-36A1-44CF-AA0E-6C1038AA1DF3", "Source3", "DisplayName3", "Description3", "Windows")]
        public async Task AddGroup_SingleGroup_SuccessAsync(string groupName, string groupSource, string displayName, string description, string identityProvider)
        {
            var postResponse = await Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = groupName,
                    GroupSource = groupSource,
                    DisplayName = displayName,
                    Description = description,
                    IdentityProvider = identityProvider
                });
            });

            var getResponse = await Browser.Get($"/groups/{groupName}", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var responseBody = getResponse.Body.AsString();
            Assert.Contains(groupName, responseBody);
            Assert.Contains(groupSource, responseBody);
            Assert.Contains(displayName, responseBody);
            Assert.Contains(description, responseBody);
            Assert.Contains(identityProvider, responseBody);
        }

        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodName]
        [InlineData("PatchGroup_ValidRequest_SuccessAsync", "Source1", "Group Display Name 1", "Group Description 1")]
        public async Task PatchGroup_ValidRequest_SuccessAsync(string groupName, string groupSource, string displayName, string description)
        {
            var postResponse = await Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = groupName,
                    GroupSource = groupSource,
                    DisplayName = displayName,
                    Description = description
                });
            });

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            var getResponse = await Browser.Get($"/groups/{groupName}", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var patchResponse = await Browser.Patch($"/groups/{groupName}", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    DisplayName = "Group Display Name 2",
                    Description = "Group Description 2"
                });
            });

            Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

            var group = JsonConvert.DeserializeObject<GroupRoleApiModel>(patchResponse.Body.AsString());
            Assert.Equal(groupName, group.GroupName);
            Assert.Equal(groupSource, group.GroupSource);
            Assert.Equal("Group Display Name 2", group.DisplayName);
            Assert.Equal("Group Description 2", group.Description);
        }

        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodName]
        [InlineData("BatchGroup1", "BatchSource1", "DisplayName1", "Description1", "Windows1")]
        [InlineData("BatchGroup2", "BatchSource2", "DisplayName2", "Description2", "Windows2")]
        [InlineData("6AC32A47-36C1-23BF-AA22-6C1028AA5DC3", "BatchSource3", "DisplayName3", "Description3", "Windows3")]
        public async Task AddGroup_Batch_SuccessAsync(string groupName, string groupSource, string displayName, string description, string identityProvider)
        {
            groupName = groupName + Guid.NewGuid();
            var postResponse = await Browser.Post("/groups/UpdateGroups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new []
                {
                    new
                    {
                        GroupName = groupName + "_0",
                        GroupSource = groupSource + "_0",
                        DisplayName = displayName + "_0",
                        Description = description + "_0",
                        IdentityProvider = identityProvider + "_0"
                    },
                    new
                    {
                        GroupName = groupName + "_1",
                        GroupSource = groupSource + "_1",
                        DisplayName = displayName + "_1",
                        Description = description + "_1",
                        IdentityProvider = identityProvider + "_1"
                    },
                    new
                    {
                        GroupName = groupName + "_2",
                        GroupSource = groupSource + "_2",
                        DisplayName = displayName + "_2",
                        Description = description + "_2",
                        IdentityProvider = identityProvider + "_2"
                    }
                });
            });

            Assert.Equal(HttpStatusCode.NoContent, postResponse.StatusCode);

            var getResponse0 = await Browser.Get($"/groups/{groupName}_0", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            });

            var getResponse1 = await Browser.Get($"/groups/{groupName}_1", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            });

            var getResponse2 = await Browser.Get($"/groups/{groupName}_2", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            });

            var response0 = getResponse0.Body.AsString();
            var response1 = getResponse1.Body.AsString();
            var response2 = getResponse2.Body.AsString();

            Assert.Equal(HttpStatusCode.OK, getResponse0.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse1.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse2.StatusCode);

            Assert.Contains(groupName + "_0", response0);
            Assert.Contains(groupName + "_1", response1);
            Assert.Contains(groupName + "_2", response2);

            Assert.Contains(groupSource + "_0", response0);
            Assert.Contains(groupSource + "_1", response1);
            Assert.Contains(groupSource + "_2", response2);

            Assert.Contains(displayName + "_0", response0);
            Assert.Contains(displayName + "_1", response1);
            Assert.Contains(displayName + "_2", response2);

            Assert.Contains(description + "_0", response0);
            Assert.Contains(description + "_1", response1);
            Assert.Contains(description + "_2", response2);

            Assert.Contains(identityProvider + "_0", response0);
            Assert.Contains(identityProvider + "_1", response1);
            Assert.Contains(identityProvider + "_2", response2);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task AddGroup_DuplicateGroupExistsAndDeleted_SuccessAsync()
        {
            string groupName = "Group1" + Guid.NewGuid();
            const string groupSource = "Custom";
            var response = await Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = groupName,
                    GroupSource = groupSource
                });
            });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            response = await Browser.Delete($"/groups/{groupName}", with =>
            {
                with.HttpRequest();
            });
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            response = await Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = groupName,
                    GroupSource = groupSource
                });
            });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodName]
        [InlineData("", "Source1")]
        [InlineData(null, "Source2")]
        public async Task AddGroup_NullOrEmptyName_BadRequestAsync(string groupName, string groupSource)
        {
            var postResponse = await Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = groupName,
                    GroupSource = groupSource
                });
            });

            Assert.Equal(HttpStatusCode.BadRequest, postResponse.StatusCode);
        }

        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodName]
        [InlineData("Source1")]
        public async Task AddGroup_MissingName_BadRequestAsync(string groupSource)
        {
            var postResponse = await Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupSource = groupSource
                });
            });

            Assert.Equal(HttpStatusCode.BadRequest, postResponse.StatusCode);
        }

        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodName]
        [InlineData("Source1", "", "")]
        [InlineData("Source2", null, null)]
        public async Task AddGroup_NullOrEmptySourceAndIdentityProvider_SuccessAsync(string groupName, string groupSource, string groupIdentityProvider)
        {
            groupName = groupName + Guid.NewGuid();
            var postResponse = await Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = groupName,
                    GroupSource = groupSource,
                    IdentityProvider = groupIdentityProvider
                });
            });

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            var getResponse = await Browser.Get($"/groups/{groupName}", with =>
            {
                with.HttpRequest();
            });

            var groupRoleApiModel = getResponse.Body.DeserializeJson<GroupRoleApiModel>();
            Assert.Equal(_defaultPropertySettings.GroupSource, groupRoleApiModel.GroupSource);
            Assert.Equal(_defaultPropertySettings.IdentityProvider, groupRoleApiModel.IdentityProvider);
        }

        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodName]
        [InlineData("Name1")]
        public async Task AddGroup_MissingSource_SuccessAsync(string groupName)
        {
            var postResponse = await Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = groupName
                });
            });

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            var getResponse = await Browser.Get($"/groups/{groupName}", with =>
            {
                with.HttpRequest();
            });

            var source = getResponse.Body.DeserializeJson<GroupRoleApiModel>().GroupSource;
            Assert.Equal(_defaultPropertySettings.GroupSource, source);
        }

        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodName]
        [InlineData("RepeatedGroup1", "Custom")]
        [InlineData("RepeatedGroup2", "Custom")]
        public async Task AddGroup_AlreadyExists_ConflictAsync(string groupName, string groupSource)
        {
            await Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = groupName,
                    GroupSource = groupSource
                });
            });

            // Repeat
            var postResponse = await Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = groupName,
                    GroupSource = groupSource
                });
            });

            Assert.Equal(HttpStatusCode.Conflict, postResponse.StatusCode);
        }

        [Theory, IntegrationTestsFixture.DisplayTestMethodName,
         InlineData("BatchUpdateGroup1", "BatchUpdateSource1", "Windows"),
         InlineData("BatchUpdateGroup2", "BatchUpdateSource2", "Windows")]
        public async Task UpdateGroup_Batch_SuccessAsync(string groupName, string groupSource, string identityProvider)
        {
            groupName = groupName + Guid.NewGuid();
            var postResponse = await Browser.Post("/groups/UpdateGroups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new
                    {
                        GroupName = groupName + "_0",
                        GroupSource = groupSource + "_0",
                        IdentityProvider = identityProvider + "_0"
                    },
                    new
                    {
                        GroupName = groupName + "_1",
                        GroupSource = groupSource + "_1",
                        IdentityProvider = identityProvider + "_1"
                    },
                    new
                    {
                        GroupName = groupName + "_2",
                        GroupSource = groupSource + "_2",
                        IdentityProvider = identityProvider + "_2"
                    }
                });

                with.Header("Accept", "application/json");
            });

            Assert.Equal(HttpStatusCode.NoContent, postResponse.StatusCode);

            // Replace groups. _0 should be removed and _3 should be added.
            postResponse = await Browser.Post("/groups/UpdateGroups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new
                    {
                        GroupName = groupName + "_1",
                        GroupSource = groupSource + "_1",
                        IdentityProvider = identityProvider + "_1"
                    },
                    new
                    {
                        GroupName = groupName + "_2",
                        GroupSource = groupSource + "_2",
                        IdentityProvider = identityProvider + "_2"
                    },
                    new
                    {
                        GroupName = groupName + "_3",
                        GroupSource = groupSource + "_3",
                        IdentityProvider = identityProvider + "_3"
                    }
                });
            });

            Assert.Equal(HttpStatusCode.NoContent, postResponse.StatusCode);

            var getResponse0 = await Browser.Get($"/groups/{groupName}_0", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            });

            var getResponse1 = await Browser.Get($"/groups/{groupName}_1", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            });

            var getResponse2 = await Browser.Get($"/groups/{groupName}_2", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            });

            var getResponse3 = await Browser.Get($"/groups/{groupName}_3", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            });

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
        public async Task DeleteGroup_SingleGroup_SuccessAsync(string groupName, string groupSource)
        {
            await Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = groupName,
                    GroupSource = groupSource
                });
            });

            var delete = await Browser.Delete($"/groups/{groupName}", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        }

        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodNameAttribute]
        [InlineData("NonexistentGroup")]
        [InlineData("NonexistentGroup2")]
        public async Task DeleteGroup_NonExistentGroup_NotFoundAsync(string groupName)
        {
            var delete = await Browser.Delete($"/groups/{groupName}", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        }

        #region Role->Group Mapping Tests

        protected async Task SetupGroupAsync(string groupName, string groupSource)
        {
            var response = await Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = groupName,
                    GroupSource = groupSource
                });
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        protected async Task<Role> SetupRoleAsync(string roleName, string grain = "app", string securableItem = "rolesprincipal")
        {
            var response = await Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = grain,
                    SecurableItem = securableItem,
                    Name = roleName
                });
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var role = response.Body.DeserializeJson<RoleApiModel>();
            var id = role.Id;

            if (id == null)
                throw new Exception("Guid not generated.");

            return role.ToRoleDomainModel();
        }

        protected async Task<BrowserResponse> SetupGroupRoleMappingAsync(string groupName, Role role, Browser browser = null)
        {
            if (browser == null)
            {
                browser = Browser;
            }

            var response = await browser.Post($"/groups/{groupName}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new
                    {
                        role.Grain,
                        role.SecurableItem,
                        role.Name,
                        role.Id
                    }
                });
            });

            return response;
        }

        [Theory]
        [MemberData(nameof(GetPrincipals))]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task AddRoleToGroup_NoPermissionsToManageSecurable_ForbiddenAsync(ClaimsPrincipal principal)
        {
            string groupName = "Group" + Guid.NewGuid();
            await SetupGroupAsync(groupName, "Custom");
            
            var role = await SetupRoleAsync("Role" + Guid.NewGuid());

            var browser = _fixture.GetBrowser(principal, _storageProvider);

            var mappingResponse = await SetupGroupRoleMappingAsync(groupName, role, browser);
            Assert.Equal(HttpStatusCode.Forbidden, mappingResponse.StatusCode);

        }

        public static IEnumerable<object[]> GetPrincipals()
        {
            return new List<object[]>
            {
                new object[] { new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                {
                    new Claim(Claims.Scope, Scopes.ManageClientsScope),
                    new Claim(Claims.Scope, Scopes.ReadScope),
                    new Claim(Claims.Scope, Scopes.WriteScope),
                    new Claim(Claims.Scope, Scopes.ManageDosScope),
                    new Claim(Claims.ClientId, "foreignclient"),
                    new Claim(Claims.Sub, "user" + Guid.NewGuid()),
                    new Claim(Claims.IdentityProvider, "idp1")
                }, "pwd")) },
                new object[] {new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                {
                    new Claim(Claims.Scope, Scopes.ManageClientsScope),
                    new Claim(Claims.Scope, Scopes.ReadScope),
                    new Claim(Claims.Scope, Scopes.WriteScope),
                    new Claim(Claims.Scope, Scopes.ManageDosScope),
                    new Claim(Claims.ClientId, "foreignclient")
                }, "pwd"))}
            };
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task AddRoleToGroup_GroupExists_SuccessAsync()
        {
            string group1Name = "Group1Name" + Guid.NewGuid();
            string role1Name = "Role1Name" + Guid.NewGuid();
            await SetupGroupAsync(group1Name, "Custom");
            var role = await SetupRoleAsync(role1Name);
            var response = await SetupGroupRoleMappingAsync(group1Name, role);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            response = await Browser.Get($"/groups/{group1Name}/roles", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseEntity = response.Body.DeserializeJson<IEnumerable<RoleApiModel>>();
            var roleList = responseEntity.ToList();
            Assert.Single(roleList);
            Assert.Equal(role1Name, roleList[0].Name);

            // set up another role->group mapping
            string group2Name = "Group2Name" + Guid.NewGuid();
            string role2Name = "Role2Name" + Guid.NewGuid();
            await SetupGroupAsync(group2Name, "Custom");
            role = await SetupRoleAsync(role2Name);
            response = await SetupGroupRoleMappingAsync(group2Name, role);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            response = await Browser.Get($"/groups/{group2Name}/roles", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            responseEntity = response.Body.DeserializeJson<IEnumerable<RoleApiModel>>();
            roleList = responseEntity.ToList();
            Assert.Single(roleList);
            Assert.Equal(role2Name, roleList[0].Name);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task AddRoleToGroup_NonExistentGroup_NotFoundAsync()
        {
            var role = await SetupRoleAsync("RoleName");
            var response = await SetupGroupRoleMappingAsync("NonexistentGroup", role);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact, IntegrationTestsFixture.DisplayTestMethodName]
        public async Task AddRoleToGroup_GroupRoleMappingAlreadyExists_AlreadyExistsExceptionAsync()
        {
            string group1Name = "Group1Name" + Guid.NewGuid();            
            await SetupGroupAsync(group1Name, "Custom");
            string role1Name = "Role1Name" + Guid.NewGuid();
            var role = await SetupRoleAsync(role1Name);
            var response = await SetupGroupRoleMappingAsync(group1Name, role);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // attempt to set up the same mapping (the API treats this as an update to the existing
            // group-role mapping)
            response = await SetupGroupRoleMappingAsync(group1Name, role);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains($"The role: {role} with Id: {role.Id} already exists for group {group1Name}", response.Body.AsString());
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task DeleteRolesFromGroup_GroupExists_SuccessAsync()
        {
            var group1Name = "Group1Name" + Guid.NewGuid();
            await SetupGroupAsync(group1Name, "Custom");
            var role1Name = "Role1Name" + Guid.NewGuid();
            var role = await SetupRoleAsync(role1Name);

            await AddAndDeleteRoleFromGroupAsync(group1Name, role);

            // execute test again to confirm we can re-add and delete the same group-role mapping a second time
            await AddAndDeleteRoleFromGroupAsync(group1Name, role);
        }

        private async Task AddAndDeleteRoleFromGroupAsync(string group1Name, Role role)
        {
            // create the mapping
            var response = await SetupGroupRoleMappingAsync(group1Name, role);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // delete the mapping
            response = await Browser.Delete($"/groups/{group1Name}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new
                    {
                        RoleId = role.Id.ToString()
                    }
                });
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            response = await Browser.Get($"/groups/{group1Name}/roles", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseEntity = response.Body.DeserializeJson<IEnumerable<RoleApiModel>>();
            var roleList = responseEntity.ToList();
            Assert.Empty(roleList);
        } 

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task DeleteRolesFromGroup_NonExistentGroup_NotFoundAsync()
        {            
            var role = await SetupRoleAsync("RoleName" + Guid.NewGuid());
            var response = await Browser.Delete("/groups/invalidGroup/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new
                    {
                        RoleId = role.Id.ToString()
                    }
                });
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task DeleteRolesFromGroup_NonExistentGroupRoleMapping_NotFoundAsync()
        {
            await SetupGroupAsync("Group1Name", "Custom");
            var response = await Browser.Delete("/groups/Group1Name/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new
                    {
                        RoleId = Guid.NewGuid().ToString()
                    }
                });
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task GetRolesForGroup_NonExistentGroup_NotFoundAsync()
        {
            var response = await Browser.Get("/groups/invalidGroup/roles", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task GetRolesForGroup_UseNameGrainAndSecurableItem_SuccessAsync()
        {

            string group1Name = "Group1Name" + Guid.NewGuid();
            string role1Name = "Role1Name" + Guid.NewGuid();
            await SetupGroupAsync(group1Name, "Custom");
            var role = await SetupRoleAsync(role1Name);
            var response = await SetupGroupRoleMappingAsync(group1Name, role);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            response = await Browser.Get($"/groups/{group1Name}/{role.Grain}/{role.SecurableItem}/roles", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseEntity = response.Body.DeserializeJson<IEnumerable<RoleApiModel>>();
        }

        #endregion

        #region User->Group Mapping Tests 

        protected async Task<BrowserResponse> SetupGroupUserMappingAsync(string groupName, string subjectId, string identityProvider, Browser browser = null)
        {
            if (browser == null)
            {
                browser = Browser;
            }

            var response = await browser.Post($"/groups/{groupName}/users", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.JsonBody(new[]
                {
                    new
                    {
                        SubjectId = subjectId,
                        IdentityProvider = identityProvider
                    }
                });
            });

            return response;
        }

        [IntegrationTestsFixture.DisplayTestMethodName]
        [Theory]
        [MemberData(nameof(GetPrincipals))]
        public async Task AddUserToGroup_NoPermissionsToRole_ForbiddenAsync(ClaimsPrincipal principal)
        {
            var group1Name = "Group1Name" + Guid.NewGuid();
            await SetupGroupAsync(group1Name, "Custom");
            
            var role = await SetupRoleAsync("Role1Name" + Guid.NewGuid());
            
            await SetupGroupRoleMappingAsync(group1Name, role);

            var browser = _fixture.GetBrowser(principal, _storageProvider);

            // Attempt to add the user to the group
            var userSubjectId = "user" + Guid.NewGuid();
            var identityProvider = "idp1";
            var mappingResponse = await SetupGroupUserMappingAsync(group1Name, userSubjectId, identityProvider, browser);
            Assert.Equal(HttpStatusCode.Forbidden, mappingResponse.StatusCode);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task AddUserToGroup_GroupExists_SuccessAsync()
        {
            string group1Name = "Group1Name" + Guid.NewGuid();
            const string user1SubjectId = "User1SubjectId";
            const string identityProvider = "idP1";

            await SetupGroupAsync(group1Name, "Custom");
            var response = await SetupGroupUserMappingAsync(group1Name, user1SubjectId, identityProvider);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            response = await Browser.Get($"/groups/{group1Name}/users", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseEntity = response.Body.DeserializeJson<IEnumerable<UserApiModel>>();
            var userList = responseEntity.ToList();
            Assert.Single(userList);
            Assert.Equal(user1SubjectId, userList[0].SubjectId);

            // set up another user->group mapping
            string group2Name = "Group2Name" + Guid.NewGuid();
            const string user2SubjectId = "User2SubjectId";

            await SetupGroupAsync(group2Name, "Custom");

            // link user 2 to group 1
            response = await SetupGroupUserMappingAsync(group1Name, user2SubjectId, identityProvider);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // link user 2 to group 2
            response = await SetupGroupUserMappingAsync(group2Name, user2SubjectId, identityProvider);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // get users for group 1
            response = await Browser.Get($"/groups/{group1Name}/users", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            responseEntity = response.Body.DeserializeJson<IEnumerable<UserApiModel>>();
            userList = responseEntity.ToList();
            Assert.Equal(2, userList.Count);

            // get users for group 2
            response = await Browser.Get($"/groups/{group2Name}/users", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            responseEntity = response.Body.DeserializeJson<IEnumerable<UserApiModel>>();
            userList = responseEntity.ToList();
            Assert.Single(userList);
            Assert.Equal(user2SubjectId, userList[0].SubjectId);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodNameAttribute]
        public async Task AddUserToGroup_NonCustomGroup_BadRequestAsync()
        {
            string group1Name = "Group1Name" + Guid.NewGuid();
            const string user1SubjectId = "User1SubjectId";

            await SetupGroupAsync(group1Name, "Active Directory");
            var response = await SetupGroupUserMappingAsync(group1Name, user1SubjectId, "idP1");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task AddUserToGroup_NoSubjectId_BadRequestAsync()
        {
            string group1Name = "Group1Name" + Guid.NewGuid();
            const string identityProvider = "idP1";

            await SetupGroupAsync(group1Name, "Custom");
            var response = await SetupGroupUserMappingAsync(group1Name, null, identityProvider);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task AddUserToGroup_NoIdentityProvider_BadRequestAsync()
        {
            string group1Name = "Group1Name" + Guid.NewGuid();
            const string user1SubjectId = "User1SubjectId";

            await SetupGroupAsync(group1Name, "Custom");
            var response = await SetupGroupUserMappingAsync(group1Name, user1SubjectId, "");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task AddUserToGroup_NonExistentGroup_NotFoundAsync()
        {
            var response = await SetupGroupUserMappingAsync("NonexistentGroup", "SubjectId", "idP1");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task AddUserToGroup_GroupUserMappingAlreadyExists_SuccessAsync()
        {
            string group1Name = "Group1Name" + Guid.NewGuid();
            const string subject1Id = "Subject1Id";
            const string identityProvider = "idP1";

            await SetupGroupAsync(group1Name, "Custom");
            var response = await SetupGroupUserMappingAsync(group1Name, subject1Id, identityProvider);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // attempt to set up the same mapping (the API treats this as an update to the existing
            // group-user mapping)
            response = await SetupGroupUserMappingAsync(group1Name, subject1Id, identityProvider);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            response = await Browser.Get($"/groups/{group1Name}/users", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseEntity = response.Body.DeserializeJson<IEnumerable<UserApiModel>>();
            var userList = responseEntity.ToList();
            Assert.Single(userList);
            Assert.Equal(subject1Id, userList[0].SubjectId);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task DeleteUserFromGroup_GroupExists_SuccessAsync()
        {
            string group1Name = "Group1Name" + Guid.NewGuid();
            await SetupGroupAsync(group1Name, "Custom");
            string subject1Id = "Subject1Id" + Guid.NewGuid();
            const string identityProvider = "idP1";
            var response = await SetupGroupUserMappingAsync(group1Name, subject1Id, identityProvider);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // delete the mapping
            response = await Browser.Delete($"/groups/{group1Name}/users", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    SubjectId = subject1Id,
                    IdentityProvider = identityProvider
                });

            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            response = await Browser.Get($"/groups/{group1Name}/users", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseEntity = response.Body.DeserializeJson<IEnumerable<UserApiModel>>();
            var userList = responseEntity.ToList();
            Assert.Empty(userList);

            // ensure the deletion is reflected in the user model
            response = await Browser.Get($"/user/{identityProvider}/{subject1Id}/groups", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var groups = response.Body.DeserializeJson<GroupUserApiModel[]>();
            Assert.Empty(groups);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task DeleteUserFromGroup_UserSoftDeletedTwice_SuccessAsync()
        {
            string group1Name = "Group1Name" + Guid.NewGuid();
            await SetupGroupAsync(group1Name, "Custom");
            string subject1Id = "Subject1Id" + Guid.NewGuid();
            const string identityProvider = "idP1";

            // Act 1 - Adding and Deleting a user in a group

            var response = await SetupGroupUserMappingAsync(group1Name, subject1Id, identityProvider);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // First deletion of User from Group
            response = await Browser.Delete($"/groups/{group1Name}/users", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    SubjectId = subject1Id,
                    IdentityProvider = identityProvider
                });

            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // ensure the deletion is reflected in the user and group user models
            response = await Browser.Get($"/groups/{group1Name}/users", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var responseEntity = response.Body.DeserializeJson<IEnumerable<UserApiModel>>();
            var userList = responseEntity.ToList();
            Assert.Empty(userList);

            response = await Browser.Get($"/user/{identityProvider}/{subject1Id}/groups", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var groups = response.Body.DeserializeJson<GroupUserApiModel[]>();
            Assert.Empty(groups);

            // Act 2 - Adding and Deleting the same user in a group

            response = await SetupGroupUserMappingAsync(group1Name, subject1Id, identityProvider);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Second deletion of User from Group
            response = await Browser.Delete($"/groups/{group1Name}/users", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    SubjectId = subject1Id,
                    IdentityProvider = identityProvider
                });

            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // ensure the deletion is reflected in the user and group user models
            response = await Browser.Get($"/groups/{group1Name}/users", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            responseEntity = response.Body.DeserializeJson<IEnumerable<UserApiModel>>();
            userList = responseEntity.ToList();
            Assert.Empty(userList);

            response = await Browser.Get($"/user/{identityProvider}/{subject1Id}/groups", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            groups = response.Body.DeserializeJson<GroupUserApiModel[]>();
            Assert.Empty(groups);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task DeleteUserFromGroup_NonExistentGroup_NotFoundAsync()
        {
            var response = await Browser.Delete("/groups/invalidGroup/users", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    SubjectId = "SubjectId",
                    IdentityProvider = "idP1"
                });
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task DeleteUserFromGroup_NonExistentGroupUserMapping_NotFoundAsync()
        {
            string group1Name = "Group1Name" + Guid.NewGuid();

            await SetupGroupAsync(group1Name, "Custom");
            var response = await Browser.Delete($"/groups/{group1Name}/users", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    SubjectId = "Subject1Id" + Guid.NewGuid(),
                    IdentityProvider = "idP1"
                });
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task DeleteUserFromGroup_NoSubjectId_BadRequestAsync()
        {
            string group1Name = "Group1Name" + Guid.NewGuid();
            await SetupGroupAsync(group1Name, "Custom");
            string subject1Id = "Subject1Id" + Guid.NewGuid();
            string identityProvider = "idP1" + Guid.NewGuid();
            var response = await SetupGroupUserMappingAsync(group1Name, subject1Id, identityProvider);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // attempt to delete the mapping
            response = await Browser.Delete($"/groups/{group1Name}/users", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    IdentityProvider = identityProvider
                });
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task DeleteUserFromGroup_NoIdentityProvider_BadRequestAsync()
        {
            string group1Name = "Group1Name" + Guid.NewGuid();
            await SetupGroupAsync(group1Name, "Custom");
            string subject1Id = "Subject1Id" + Guid.NewGuid();
            string identityProvider = "idP1" + Guid.NewGuid();
            var response = await SetupGroupUserMappingAsync(group1Name, subject1Id, identityProvider);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // attempt to delete the mapping
            response = await Browser.Delete($"/groups/{group1Name}/users", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    SubjectId = subject1Id,
                });
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task GetUsersForGroup_NonExistentGroup_NotFoundAsync()
        {
            var response = await Browser.Get("/groups/invalidGroup/users", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task GetGroupsForUser_GroupAndUserExist_SuccessAsync()
        {
            string groupName = "GroupName" + Guid.NewGuid();
            string subjectId = "Subject1Id" + Guid.NewGuid();
            string identityProvider = "idP1" + Guid.NewGuid();
            await SetupGroupAsync(groupName, "Custom");
            await SetupGroupUserMappingAsync(groupName, subjectId, identityProvider);

            var response = await Browser.Get($"/user/{identityProvider}/{subjectId}/groups", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var groupList = response.Body.DeserializeJson<GroupUserApiModel[]>();
            Assert.Single(groupList);
            Assert.Equal(groupName, groupList[0].GroupName);
        }

        #endregion

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task GetGroups_AddPermissionToRole_AllGroupsSyncedAsync()
        {
            string groupName = "Admin" + Guid.NewGuid();
            string roleName = "Administrator" + Guid.NewGuid();
            string permissionName = "app-write" + Guid.NewGuid();

            // create group
            var postResponse = await Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = groupName,
                    GroupSource = "Custom"
                });
            });

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            // create role
            postResponse = await Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = "rolesprincipal",
                    Name = roleName
                });
            });

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            var role = postResponse.Body.DeserializeJson<RoleApiModel>();
            var roleId = role.Id.ToString();

            // add role to group
            postResponse = await SetupGroupRoleMappingAsync(groupName, role.ToRoleDomainModel());

            Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

            // add permission
            postResponse = await Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = "rolesprincipal",
                    Name = permissionName
                });
            });

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
            postResponse = await Browser.Post($"/roles/{roleId}/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(permissionApiModels);
            });

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            await VerifyPermissionAsync(groupName, roleName, permissionName, true);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task GetGroups_DeletePermissionFromRole_AllGroupsSyncedAsync()
        {
            string groupName = "Admin" + Guid.NewGuid();
            const string roleName = "Administrator";
            const string permissionName = "app-write";

            // create group
            var postResponse = await Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = groupName,
                    GroupSource = "Custom"
                });
            });

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            // create role
            postResponse = await Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = "rolesprincipal",
                    Name = roleName
                });
            });

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            var role = postResponse.Body.DeserializeJson<RoleApiModel>();
            var roleId = role.Id.ToString();

            // add role to group
            postResponse = await SetupGroupRoleMappingAsync(groupName, role.ToRoleDomainModel());

            Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

            // add permission
            postResponse = await Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = "rolesprincipal",
                    Name = permissionName
                });
            });

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
            postResponse = await Browser.Post($"/roles/{roleId}/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(permissionApiModels);
            });

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            await VerifyPermissionAsync(groupName, roleName, permissionName, true);

            // delete permission from role
            postResponse = await Browser.Delete($"/roles/{roleId}/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(permissionApiModels);
            });

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            await VerifyPermissionAsync(groupName, roleName, permissionName, false);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task SearchGroups_CustomGroupOnly_SuccessAsync()
        {
            string customGroup = "CustomGroupSearch" + Guid.NewGuid();
            await SetupGroupAsync(customGroup, "custom");
            string directoryGroup = "CustomGroupSearch" + Guid.NewGuid();
            await SetupGroupAsync(directoryGroup, GroupConstants.DirectorySource);

            var response = await Browser.Get($"/groups", with =>
            {
                with.HttpRequest();
                with.Query("name", "CustomGroup");
                with.Query("type", "custom");
                with.Header("Accept", "application/json");
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var groupList = response.Body.DeserializeJson<GroupRoleApiModel[]>();
            Assert.Single(groupList);
            Assert.Equal("custom", groupList[0].GroupSource);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task SearchGroups_DirectoryGroupOnly_SuccessAsync()
        {
            string customGroup = "DirectoryGroupSearch" + Guid.NewGuid();
            await SetupGroupAsync(customGroup, "custom");
            string directoryGroup = "DirectoryGroupSearch" + Guid.NewGuid();
            await SetupGroupAsync(directoryGroup, GroupConstants.DirectorySource);

            var response = await Browser.Get($"/groups", with =>
            {
                with.HttpRequest();
                with.Query("name", "DirectoryGroup");
                with.Query("type", "directory");
                with.Header("Accept", "application/json");
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var groupList = response.Body.DeserializeJson<GroupRoleApiModel[]>();
            Assert.Single(groupList);
            Assert.NotEqual("custom", groupList[0].GroupSource);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task SearchGroups_NoTypeProvided_ReturnsAllTypes_SuccessAsync()
        {
            string customGroup = "AllGroupSearch" + Guid.NewGuid();
            await SetupGroupAsync(customGroup, "custom");
            string directoryGroup = "AllGroupSearch" + Guid.NewGuid();
            await SetupGroupAsync(directoryGroup, GroupConstants.DirectorySource);

            var response = await Browser.Get($"/groups", with =>
            {
                with.HttpRequest();
                with.Query("name", "AllGroup");                
                with.Header("Accept", "application/json");
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var groupList = response.Body.DeserializeJson<GroupRoleApiModel[]>();
            Assert.Equal(2, groupList.Length);            
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task SearchGroups_InvalidType_BadRequest_Async()
        {
            var response = await Browser.Get($"/groups", with =>
            {
                with.HttpRequest();
                with.Query("name", "AllGroup");
                with.Query("type", "foo");
                with.Header("Accept", "application/json");
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task SearchGroups_NoName_BadRequest_Async()
        {
            var response = await Browser.Get($"/groups", with =>
            {
                with.HttpRequest();                
                with.Query("type", "foo");
                with.Header("Accept", "application/json");
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // ReSharper disable once UnusedParameter.Local
        private async Task VerifyPermissionAsync(string groupName, string roleName, string permissionName, bool exists)
        {
            // get the group
            var getResponse = await Browser.Get($"/groups/{groupName}", with =>
            {
                with.HttpRequest();
            });

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