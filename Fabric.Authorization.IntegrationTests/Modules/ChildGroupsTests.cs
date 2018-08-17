using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
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

        protected async Task<Role> SetupRoleAsync(string roleName, string grain = "app", string securableItem = "rolesprincipal")
        {
            var response = await _browser.Post("/roles", with =>
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

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

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
            var role = await SetupRoleAsync("Role" + Guid.NewGuid(), "dos");

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

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

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
        }
    }
}
