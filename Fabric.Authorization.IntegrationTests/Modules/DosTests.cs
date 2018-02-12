using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Models;
using Nancy;
using Nancy.Testing;
using Newtonsoft.Json;
using Xunit;

namespace Fabric.Authorization.IntegrationTests.Modules
{
    public class DosTests : IClassFixture<IntegrationTestsFixture>
    {
        private readonly IntegrationTestsFixture _fixture;
        private readonly string _storageProvider;
        public DosTests(IntegrationTestsFixture fixture, string storageProvider = StorageProviders.InMemory)
        {
            _fixture = fixture;
            _storageProvider = storageProvider;
        }

        [Fact]
        public async Task AddDosPermission_UserInRole_SuccessAsync()
        {
            var user = "user" + Guid.NewGuid();
            await AssociateUserToDosAdminRoleAsync(user);

            var clientId = "clientid" + Guid.NewGuid();
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(Claims.Scope, Scopes.ManageClientsScope),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.Scope, Scopes.ManageDosScope),
                new Claim(Claims.ClientId, clientId),
                new Claim(Claims.Sub, user),
                new Claim(Claims.IdentityProvider, "Windows")
            }, "pwd"));

            var browser = _fixture.GetBrowser(principal, _storageProvider);
            var permission = "permission" + Guid.NewGuid();
            var postResponse = await browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "dos",
                    SecurableItem = "datamarts",
                    Name = permission
                });
            });
            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            var createdPermission = JsonConvert.DeserializeObject<PermissionApiModel>(postResponse.Body.AsString());

            var permissionResponse = await browser.Get($"/permissions/{createdPermission.Id}", with =>
            {
                with.HttpRequest();
            });
            Assert.Equal(HttpStatusCode.OK, permissionResponse.StatusCode);
            var retrievedPermission =
                JsonConvert.DeserializeObject<PermissionApiModel>(permissionResponse.Body.AsString());

            Assert.Equal(createdPermission.Id, retrievedPermission.Id);
        }

        [Theory]
        [InlineData("fabric-installer")]
        [InlineData("dos-metadata-service")]
        public async Task AddDosPermission_AllowedApp_SuccessAsync(string clientId)
        {
            //var clientId = "fabric-installer";
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(Claims.Scope, Scopes.ManageClientsScope),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.ClientId, clientId)
            }, "pwd"));

            var browser = _fixture.GetBrowser(principal, _storageProvider);

            var permission = "permission" + Guid.NewGuid();
            var postResponse = await browser.Post("/permissions", with =>
                {
                    with.HttpRequest();
                    with.JsonBody(new
                    {
                        Grain = "dos",
                        SecurableItem = "datamarts",
                        Name = permission
                    });
                });

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            var createdPermission = JsonConvert.DeserializeObject<PermissionApiModel>(postResponse.Body.AsString());

            Assert.Equal(permission, createdPermission.Name);
            Assert.Equal("dos", createdPermission.Grain);
            Assert.Equal("datamarts", createdPermission.SecurableItem);

            // Get by name
            var getResponse = await browser.Get($"/permissions/{createdPermission.Id}", with =>
                {
                    with.HttpRequest();
                });

            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            var retrievedPermission = JsonConvert.DeserializeObject<PermissionApiModel>(getResponse.Body.AsString());
            
            Assert.Equal(permission, retrievedPermission.Name);
            Assert.Equal("dos", retrievedPermission.Grain);
            Assert.Equal("datamarts", retrievedPermission.SecurableItem);
        }

        [Fact]
        public async Task AddDosPermission_UserNotInRole_ForbiddenAsync()
        {
            var sub = "sub" + Guid.NewGuid();
            var clientId = "clientid" + Guid.NewGuid();
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(Claims.Scope, Scopes.ManageClientsScope),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.Scope, Scopes.ManageDosScope),
                new Claim(Claims.ClientId, clientId),
                new Claim(Claims.Sub, sub),
                new Claim(Claims.IdentityProvider, "Windows")
            }, "pwd"));

            var browser = _fixture.GetBrowser(principal, _storageProvider);

            var permission = "permission" + Guid.NewGuid();
            var postResponse = await browser.Post("/permissions", with =>
                {
                    with.HttpRequest();
                    with.JsonBody(new
                    {
                        Grain = "dos",
                        SecurableItem = "datamarts",
                        Name = permission
                    });
                });

            // Get by name
            var getResponse = await browser.Get($"/permissions/dos/datamarts/{permission}", with =>
                {
                    with.HttpRequest();
                });

            var permissionsAsString = getResponse.Body.AsString();

            Assert.Equal(HttpStatusCode.Forbidden, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.Equal("[]", permissionsAsString);
        }

        [Fact]
        public async Task AddDosPermission_UserInRole_MissingScope_ForbiddenAsync()
        {
            var user = "user" + Guid.NewGuid();
            await AssociateUserToDosAdminRoleAsync(user);

            var clientId = "clientid" + Guid.NewGuid();
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(Claims.Scope, Scopes.ManageClientsScope),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.ClientId, clientId),
                new Claim(Claims.Sub, user),
                new Claim(Claims.IdentityProvider, "Windows")
            }, "pwd"));

            var browser = _fixture.GetBrowser(principal, _storageProvider);
            var permission = "permission" + Guid.NewGuid();
            var postResponse = await browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "dos",
                    SecurableItem = "datamarts",
                    Name = permission
                });
            });
            Assert.Equal(HttpStatusCode.Forbidden, postResponse.StatusCode);
        }

        [Fact]
        public async Task AddDosPermission_IncorrectClient_ForbiddenAsync()
        {
            var clientId = "not-fabric-installer";
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(Claims.Scope, Scopes.ManageClientsScope),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.Scope, Scopes.ManageDosScope),
                new Claim(Claims.ClientId, clientId)
            }, "pwd"));

            var browser = _fixture.GetBrowser(principal, _storageProvider);

            var permission = "permission" + Guid.NewGuid();
            var postResponse = await browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "dos",
                    SecurableItem = "datamarts",
                    Name = permission
                });
            });

            Assert.Equal(HttpStatusCode.Forbidden, postResponse.StatusCode);
        }

        [Fact]
        public async Task AddDosPermission_WrongSecurable_BadRequestAsync()
        {
            var clientId = "fabric-installer";
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(Claims.Scope, Scopes.ManageClientsScope),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.Scope, Scopes.ManageDosScope),
                new Claim(Claims.ClientId, clientId)
            }, "pwd"));

            var browser = _fixture.GetBrowser(principal, _storageProvider);

            var permission = "permission" + Guid.NewGuid();
            var postResponse = await browser.Post("/permissions", with =>
                {
                    with.HttpRequest();
                    with.JsonBody(new
                    {
                        Grain = "dos",
                        SecurableItem = "badsecurable",
                        Name = permission
                    });
                });

            Assert.Equal(HttpStatusCode.BadRequest, postResponse.StatusCode);
        }

        private async Task AssociateUserToDosAdminRoleAsync(string user)
        {
            var clientId = "fabric-installer";
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(Claims.Scope, Scopes.ManageClientsScope),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.Scope, Scopes.ManageDosScope),
                new Claim(Claims.ClientId, clientId)
            }, "pwd"));

            var browser = _fixture.GetBrowser(principal, _storageProvider);

            var roleResponse = await browser.Get("/roles/dos/datamarts/dosadmin", with =>
                {
                    with.HttpRequest();
                });
            Assert.Equal(HttpStatusCode.OK, roleResponse.StatusCode);
            var role = JsonConvert.DeserializeObject<List<RoleApiModel>>(roleResponse.Body.AsString()).First();
            Assert.Equal("dosadmin", role.Name);

            var groupResponse = await browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = "dosadmins" + Guid.NewGuid(),
                    GroupSource = "Custom"
                });
            });
            Assert.Equal(HttpStatusCode.Created, groupResponse.StatusCode);
            var group = JsonConvert.DeserializeObject<GroupRoleApiModel>(groupResponse.Body.AsString());

            var groupRoleResponse = await browser.Post($"/groups/{group.GroupName}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    RoleId = role.Id
                });
            });
            Assert.Equal(HttpStatusCode.Created, groupRoleResponse.StatusCode);

            var groupUserResponse = await browser.Post($"/groups/{group.GroupName}/users", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = group.GroupName,
                    SubjectId = user,
                    IdentityProvider = "Windows"
                });
            });
            Assert.Equal(HttpStatusCode.Created, groupUserResponse.StatusCode);
        }
    }
}
