using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Catalyst.Fabric.Authorization.Models;
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
    public class ChildGroupsTests : IClassFixture<IntegrationTestsFixture>
    {
        private readonly Browser _browser;
        private readonly IntegrationTestsFixture _fixture;
        private readonly string _storageProvider;

        protected ClaimsPrincipal Principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
        {
            new Claim(Claims.Scope, Scopes.ManageClientsScope),
            new Claim(Claims.Scope, Scopes.ReadScope),
            new Claim(Claims.Scope, Scopes.WriteScope),
            new Claim(Claims.Scope, Scopes.ManageDosScope),
            new Claim(Claims.ClientId, "rolesprincipal"),
            new Claim(Claims.IdentityProvider, "idP1")
        }, "rolesprincipal"));

        public ChildGroupsTests(IntegrationTestsFixture fixture, string storageProvider = StorageProviders.InMemory, ConnectionStrings connectionStrings = null)
        {
            if (connectionStrings != null)
            {
                fixture.ConnectionStrings = connectionStrings;
            }
            _browser = fixture.GetBrowser(Principal, storageProvider);
            fixture.CreateClient(_browser, "rolesprincipal");
            _fixture = fixture;
            _storageProvider = storageProvider;
        }

        protected async Task<BrowserResponse> SetupGroupRoleMappingAsync(string groupName, Role role, Browser browser = null)
        {
            if (browser == null)
            {
                browser = _browser;
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

        protected async Task<Role> SetupRoleAndPermissionAsync(string roleName, IEnumerable<PermissionApiModel> permissionApiModels = null, string grain = "app", string securableItem = "rolesprincipal")
        {
            BrowserResponse response;
            if (permissionApiModels == null)
            {
                response = await _browser.Post("/roles", with =>
                {
                    with.HttpRequest();
                    with.JsonBody(new
                    {
                        Grain = grain,
                        SecurableItem = securableItem,
                        Name = roleName
                    });
                });
            }
            else
            {
                response = await _browser.Post("/roles", with =>
                {
                    with.HttpRequest();
                    with.JsonBody(new
                    {
                        Grain = grain,
                        SecurableItem = securableItem,
                        Name = roleName,
                        Permissions = permissionApiModels
                    });
                });
            }

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var role = response.Body.DeserializeJson<RoleApiModel>();
            var id = role.Id;

            if (id == null)
                throw new Exception("Guid not generated.");

            return role.ToRoleDomainModel();
        }

        private async Task<GroupRoleApiModel> SetupGroupAsync(string groupName, string groupSource, string displayName, string description)
        {
            var postResponse = await _browser.Post("/groups", with =>
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

            return JsonConvert.DeserializeObject<GroupRoleApiModel>(postResponse.Body.AsString());
        }

        private async Task<PermissionApiModel> SetupPermissionAsync(string grain, string securableItem, string permissionName)
        {
            var post = await _browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = grain,
                    SecurableItem = securableItem,
                    Name = permissionName
                });
            });

            Assert.Equal(HttpStatusCode.Created, post.StatusCode);
            return JsonConvert.DeserializeObject<PermissionApiModel>(post.Body.AsString());
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task AddChildGroup_ValidRequest_SuccessAsync()
        {
            var parentGroup = await SetupGroupAsync(Guid.NewGuid().ToString(), GroupConstants.CustomSource, "Custom Parent Group", "Custom Parent Group");
            var childGroup1 = await SetupGroupAsync(Guid.NewGuid().ToString(), GroupConstants.DirectorySource, "Child Group 1", "Child Group 1");
            var childGroup2 = await SetupGroupAsync(Guid.NewGuid().ToString(), GroupConstants.DirectorySource, "Child Group 2", "Child Group 2");

            var postResponse = await _browser.Post($"/groups/{parentGroup.GroupName}/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new { childGroup1.GroupName },
                    new { childGroup2.GroupName }
                });
            });

            Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

            var groupApiModel = JsonConvert.DeserializeObject<GroupRoleApiModel>(postResponse.Body.AsString());
            Assert.NotNull(groupApiModel);

            Assert.Equal(2, groupApiModel.Children.Count());
            Assert.Contains(groupApiModel.Children, c => c.GroupName == childGroup1.GroupName);
            Assert.Contains(groupApiModel.Children, c => c.GroupName == childGroup2.GroupName);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task AddChildGroup_NonExistentChildGroup_NotFoundAsync()
        {
            var parentGroup = await SetupGroupAsync(Guid.NewGuid().ToString(), GroupConstants.CustomSource, "Custom Parent Group", "Custom Parent Group");

            var postResponse = await _browser.Post($"/groups/{parentGroup.GroupName}/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new { GroupName = "DoesNotExist" }
                });
            });

            Assert.Equal(HttpStatusCode.NotFound, postResponse.StatusCode);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task AddChildGroup_NonCustomParentGroup_BadRequestAsync()
        {
            var parentGroup = await SetupGroupAsync(Guid.NewGuid().ToString(), GroupConstants.DirectorySource, "Parent Group", "Parent Group");
            var childGroup1 = await SetupGroupAsync(Guid.NewGuid().ToString(), GroupConstants.DirectorySource, "Child Group 1", "Child Group 1");
            var childGroup2 = await SetupGroupAsync(Guid.NewGuid().ToString(), GroupConstants.DirectorySource, "Child Group 2", "Child Group 2");

            var postResponse = await _browser.Post($"/groups/{parentGroup.GroupName}/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new { childGroup1.GroupName },
                    new { childGroup2.GroupName }
                });
            });


            Assert.Equal(HttpStatusCode.BadRequest, postResponse.StatusCode);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task AddChildGroup_CustomChildGroup_BadRequestAsync()
        {
            var parentGroup = await SetupGroupAsync(Guid.NewGuid().ToString(), GroupConstants.CustomSource, "Custom Parent Group", "Custom Parent Group");
            var childGroup1 = await SetupGroupAsync(Guid.NewGuid().ToString(), GroupConstants.CustomSource, "Child Group 1", "Child Group 1");
            var childGroup2 = await SetupGroupAsync(Guid.NewGuid().ToString(), GroupConstants.DirectorySource, "Child Group 2", "Child Group 2");

            var postResponse = await _browser.Post($"/groups/{parentGroup.GroupName}/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new { childGroup1.GroupName },
                    new { childGroup2.GroupName }
                });
            });

            Assert.Equal(HttpStatusCode.BadRequest, postResponse.StatusCode);
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
                    new Claim(Claims.ClientId, "rolesprincipal"),
                    new Claim(Claims.Sub, "user" + Guid.NewGuid()),
                    new Claim(Claims.IdentityProvider, "idp1")
                }, "pwd")) },
                new object[] {new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                {
                    new Claim(Claims.Scope, Scopes.ManageClientsScope),
                    new Claim(Claims.Scope, Scopes.ReadScope),
                    new Claim(Claims.Scope, Scopes.WriteScope),
                    new Claim(Claims.ClientId, "rolesprincipal")
                }, "pwd"))}
            };
        }

        [Theory]
        [MemberData(nameof(GetPrincipals))]
        public async Task AddChildGroup_InsufficientPermissions_ForbiddenAsync(ClaimsPrincipal principal)
        {
            var parentGroup = await SetupGroupAsync(Guid.NewGuid().ToString(), GroupConstants.CustomSource, "Custom Parent Group", "Custom Parent Group");
            var role = await SetupRoleAndPermissionAsync("Role" + Guid.NewGuid(), new List<PermissionApiModel>(), "dos");

            var mappingResponse = await SetupGroupRoleMappingAsync(parentGroup.GroupName, role);
            Assert.Equal(HttpStatusCode.OK, mappingResponse.StatusCode);

            var childGroup1 = await SetupGroupAsync(Guid.NewGuid().ToString(), GroupConstants.CustomSource, "Child Group 1", "Child Group 1");
            var childGroup2 = await SetupGroupAsync(Guid.NewGuid().ToString(), GroupConstants.DirectorySource, "Child Group 2", "Child Group 2");

            // get a new browser instance with insufficient permissions
            var browser = _fixture.GetBrowser(principal, _storageProvider);
            var postResponse = await browser.Post($"/groups/{parentGroup.GroupName}/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new { childGroup1.GroupName },
                    new { childGroup2.GroupName }
                });
            });

            Assert.Equal(HttpStatusCode.Forbidden, postResponse.StatusCode);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task AddChildGroup_ChildGroupExists_ConflictAsync()
        {
            var parentGroup = await SetupGroupAsync(Guid.NewGuid().ToString(), GroupConstants.CustomSource, "Custom Parent Group", "Custom Parent Group");
            var childGroup1 = await SetupGroupAsync(Guid.NewGuid().ToString(), GroupConstants.DirectorySource, "Child Group 1", "Child Group 1");
            var childGroup2 = await SetupGroupAsync(Guid.NewGuid().ToString(), GroupConstants.DirectorySource, "Child Group 2", "Child Group 2");

            var postResponse = await _browser.Post($"/groups/{parentGroup.GroupName}/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new { childGroup1.GroupName },
                    new { childGroup2.GroupName }
                });
            });

            Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

            var groupApiModel = JsonConvert.DeserializeObject<GroupRoleApiModel>(postResponse.Body.AsString());
            Assert.NotNull(groupApiModel);

            Assert.Equal(2, groupApiModel.Children.Count());
            Assert.Contains(groupApiModel.Children, c => c.GroupName == childGroup1.GroupName);
            Assert.Contains(groupApiModel.Children, c => c.GroupName == childGroup2.GroupName);

            postResponse = await _browser.Post($"/groups/{parentGroup.GroupName}/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new { childGroup1.GroupName },
                    new { childGroup2.GroupName }
                });
            });

            Assert.Equal(HttpStatusCode.Conflict, postResponse.StatusCode);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task GetUserPermissions_WithChildGroups_SuccessAsync()
        {
            var parentPermission = await SetupPermissionAsync("app", "rolesprincipal", Guid.NewGuid().ToString());
            var childPermission1 = await SetupPermissionAsync("app", "rolesprincipal", Guid.NewGuid().ToString());
            var childPermission2 = await SetupPermissionAsync("app", "rolesprincipal", Guid.NewGuid().ToString());

            // create parent group
            var parentGroup = await SetupGroupAsync(Guid.NewGuid().ToString(), GroupConstants.CustomSource, "Custom Parent Group", "Custom Parent Group");
            var parentRole = await SetupRoleAndPermissionAsync("Role" + Guid.NewGuid(), new List<PermissionApiModel> {parentPermission});
            var mappingResponse = await SetupGroupRoleMappingAsync(parentGroup.GroupName, parentRole);
            Assert.Equal(HttpStatusCode.OK, mappingResponse.StatusCode);

            // create child groups
            var childGroup1 = await SetupGroupAsync(Guid.NewGuid().ToString(), GroupConstants.DirectorySource, "Child Group 1", "Child Group 1");
            var childRole1 = await SetupRoleAndPermissionAsync("Role" + Guid.NewGuid(), new List<PermissionApiModel> { childPermission1 });
            mappingResponse = await SetupGroupRoleMappingAsync(childGroup1.GroupName, childRole1);
            Assert.Equal(HttpStatusCode.OK, mappingResponse.StatusCode);

            var childGroup2 = await SetupGroupAsync(Guid.NewGuid().ToString(), GroupConstants.DirectorySource, "Child Group 2", "Child Group 2");
            var childRole2 = await SetupRoleAndPermissionAsync("Role" + Guid.NewGuid(), new List<PermissionApiModel> { childPermission2 });
            mappingResponse = await SetupGroupRoleMappingAsync(childGroup2.GroupName, childRole2);
            Assert.Equal(HttpStatusCode.OK, mappingResponse.StatusCode);

            // add child groups to parent
            var postResponse = await _browser.Post($"/groups/{parentGroup.GroupName}/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new { childGroup1.GroupName },
                    new { childGroup2.GroupName }
                });
            });

            Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

            // add user to parent group
            var subjectId = "bob.smith";
            var idP = "Windows";

            var userGroupResponse = await _browser.Post($"/groups/{parentGroup.GroupName}/users", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new
                    {
                        SubjectId = subjectId,
                        IdentityProvider = idP
                    }
                });
            });

            Assert.Equal(HttpStatusCode.OK, userGroupResponse.StatusCode);

            // create a principal w/ the user created above
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(Claims.Scope, Scopes.ManageClientsScope),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.Sub, subjectId),
                new Claim(Claims.IdentityProvider, idP),
                new Claim(Claims.ClientId, "rolesprincipal")
            }, "pwd"));

            var browser = _fixture.GetBrowser(principal, _storageProvider);

            // get authenticated user's permissions
            var get = await browser.Get("/user/permissions", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            var userPermissionsApiModel = get.Body.DeserializeJson<UserPermissionsApiModel>();
            var userPermissions = userPermissionsApiModel.Permissions.ToList();
            Assert.Equal(3, userPermissions.Count);
            Assert.Contains(parentPermission.ToString(), userPermissions);
            Assert.Contains(childPermission1.ToString(), userPermissions);
            Assert.Contains(childPermission2.ToString(), userPermissions);

            // get non-authenticated user's permissions
            get = await _browser.Get($"/user/{idP}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();
            });

            userPermissions = get.Body.DeserializeJson<IEnumerable<ResolvedPermissionApiModel>>().Select(p => p.ToString()).ToList();
            Assert.Equal(3, userPermissions.Count);
            Assert.Contains(parentPermission.ToString(), userPermissions);
            Assert.Contains(childPermission1.ToString(), userPermissions);
            Assert.Contains(childPermission2.ToString(), userPermissions);
        }
    }
}
