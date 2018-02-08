using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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
            Console.WriteLine($"DosTests ctor for storage provider: {storageProvider}");
            _fixture = fixture;
            _storageProvider = storageProvider;
        }

        //[Fact]
        public void AddDosPermission_UserInRole_Success()
        {
            var user = "user" + Guid.NewGuid();
            AssociateUserToDosAdminRole(user);

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
            var postResponse = browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "dos",
                    SecurableItem = "datamarts",
                    Name = permission
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            var createdPermission = JsonConvert.DeserializeObject<PermissionApiModel>(postResponse.Body.AsString());

            var permissionResponse = browser.Get($"/permissions/{createdPermission.Id}", with =>
            {
                with.HttpRequest();
            }).Result;

            Assert.Equal(HttpStatusCode.OK, permissionResponse.StatusCode);
            var retrievedPermission =
                JsonConvert.DeserializeObject<PermissionApiModel>(permissionResponse.Body.AsString());

            Assert.Equal(createdPermission.Id, retrievedPermission.Id);

        }

        //[Fact]
        public void AddDosPermission_Installer_Success()
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
            var postResponse = browser.Post("/permissions", with =>
                {
                    with.HttpRequest();
                    with.JsonBody(new
                    {
                        Grain = "dos",
                        SecurableItem = "datamarts",
                        Name = permission
                    });
                })
                .Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            var createdPermission = JsonConvert.DeserializeObject<PermissionApiModel>(postResponse.Body.AsString());

            Assert.Equal(permission, createdPermission.Name);
            Assert.Equal("dos", createdPermission.Grain);
            Assert.Equal("datamarts", createdPermission.SecurableItem);

            // Get by name
            var getResponse = browser.Get($"/permissions/{createdPermission.Id}", with =>
                {
                    with.HttpRequest();
                })
                .Result;

            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            var retrievedPermission = JsonConvert.DeserializeObject<PermissionApiModel>(getResponse.Body.AsString());
            
            Assert.Equal(permission, retrievedPermission.Name);
            Assert.Equal("dos", retrievedPermission.Grain);
            Assert.Equal("datamarts", retrievedPermission.SecurableItem);
        }

        //[Fact]
        public void AddDosPermission_UserNotInRole_Forbidden()
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
            var postResponse = browser.Post("/permissions", with =>
                {
                    with.HttpRequest();
                    with.JsonBody(new
                    {
                        Grain = "dos",
                        SecurableItem = "datamarts",
                        Name = permission
                    });
                }).Result;

            // Get by name
            var getResponse = browser.Get($"/permissions/dos/datamarts/{permission}", with =>
                {
                    with.HttpRequest();
                })
                .Result;

            var permissionsAsString = getResponse.Body.AsString();

            Assert.Equal(HttpStatusCode.Forbidden, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.Equal("[]", permissionsAsString);
        }

        //[Fact]
        public void AddDosPermission_MissingScope_Forbidden()
        {
            var user = "user" + Guid.NewGuid();
            AssociateUserToDosAdminRole(user);

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
            var postResponse = browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "dos",
                    SecurableItem = "datamarts",
                    Name = permission
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Forbidden, postResponse.StatusCode);
        }

        //[Fact]
        public void AddDosPermission_IncorrectClient_Forbidden()
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
            var postResponse = browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "dos",
                    SecurableItem = "datamarts",
                    Name = permission
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Forbidden, postResponse.StatusCode);
        }

        //[Fact]
        public void AddDosPermission_WrongSecurable_Forbidden()
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
            var postResponse = browser.Post("/permissions", with =>
                {
                    with.HttpRequest();
                    with.JsonBody(new
                    {
                        Grain = "dos",
                        SecurableItem = "badsecurable",
                        Name = permission
                    });
                })
                .Result;

            Assert.Equal(HttpStatusCode.Forbidden, postResponse.StatusCode);
        }

        private void AssociateUserToDosAdminRole(string user)
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

            var roleResponse = browser.Get("/roles/dos/datamarts/dosadmin", with =>
                {
                    with.HttpRequest();
                })
                .Result;

            Assert.Equal(HttpStatusCode.OK, roleResponse.StatusCode);
            var role = JsonConvert.DeserializeObject<List<RoleApiModel>>(roleResponse.Body.AsString()).First();
            Assert.Equal("dosadmin", role.Name);

            var groupResponse = browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = "dosadmins" + Guid.NewGuid(),
                    GroupSource = "Custom"
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Created, groupResponse.StatusCode);
            var group = JsonConvert.DeserializeObject<GroupRoleApiModel>(groupResponse.Body.AsString());

            var groupRoleResponse = browser.Post($"/groups/{group.GroupName}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    RoleId = role.Id
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Created, groupRoleResponse.StatusCode);

            var groupUserResponse = browser.Post($"/groups/{group.GroupName}/users", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = group.GroupName,
                    SubjectId = user,
                    IdentityProvider = "Windows"
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Created, groupUserResponse.StatusCode);
        }
    }
}
