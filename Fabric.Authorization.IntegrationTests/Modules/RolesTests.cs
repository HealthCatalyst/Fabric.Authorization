using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.Persistence.SqlServer.Configuration;
using Nancy;
using Nancy.Testing;

using Newtonsoft.Json;

using Xunit;

namespace Fabric.Authorization.IntegrationTests.Modules
{
    [Collection("InMemoryTests")]
    public class RolesTests : IClassFixture<IntegrationTestsFixture>
    {
        protected readonly Browser _browser;
        protected readonly string _securableItem;

        public RolesTests(IntegrationTestsFixture fixture, string storageProvider = StorageProviders.InMemory, ConnectionStrings connectionStrings = null)
        {
            if (connectionStrings != null)
            {
                fixture.ConnectionStrings = connectionStrings;
            }
            _securableItem = "rolesprincipal" + Guid.NewGuid();

            var principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(Claims.Scope, Scopes.ManageClientsScope),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.ClientId, _securableItem)
            }, _securableItem));

            _browser = fixture.GetBrowser(principal, storageProvider);
            fixture.CreateClient(_browser, _securableItem);
        }

        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodName]
        [InlineData("C5247AA4-0063-4E68-B1E4-55BD5E0E177D")]
        [InlineData("C5247AA4-0063-4E68-B1E4-55BD5E0E177E")]
        public async Task TestGetRole_FailAsync(string name)
        {
            var get = await _browser.Get($"/roles/app/{_securableItem}/{name}", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.OK, get.StatusCode); //TODO: Should it be NotFound?
            Assert.True(!get.Body.AsString().Contains(name));
        }

        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodName]
        [InlineData("EA318378-CCA3-42B4-93E2-F2FBF12E679A", "Role Display Name 1", "Role Description 1")]
        [InlineData("2374EEB4-EC72-454D-915B-23A89AD67879", "Role Display Name 2", "Role Description 2")]
        [InlineData("6BC32347-36A1-44CF-AA0E-6C1038AA1DF3", "Role Display Name 3", "Role Description 3")]
        public async Task AddRole_ValidRequest_SuccessAsync(string name, string displayName, string description)
        {
            var postResponse = await _browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = name,
                    DisplayName = displayName,
                    Description = description
                });
            });

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            var getResponse = await _browser.Get($"/roles/app/{_securableItem}/{name}", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var roles = JsonConvert.DeserializeObject<List<RoleApiModel>>(getResponse.Body.AsString());

            Assert.Single(roles);
            var role = roles.First();
            Assert.Equal(name, role.Name);
            Assert.Equal(displayName, role.DisplayName);
            Assert.Equal(description, role.Description);
        }

        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodName]
        [InlineData("EA318378-CCA3-42B4-93E2-F2FBF12E679A", "Role Display Name 1", "Role Description 1")]
        public async Task PatchRole_ValidRequest_SuccessAsync(string name, string displayName, string description)
        {
            var postResponse = await _browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = name,
                    DisplayName = displayName,
                    Description = description
                });
            });

            var getResponse = await _browser.Get($"/roles/app/{_securableItem}/{name}", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var roles = JsonConvert.DeserializeObject<List<RoleApiModel>>(getResponse.Body.AsString());

            Assert.Single(roles);
            var role = roles.First();

            var patchResponse = await _browser.Patch($"/roles/{role.Id}", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    DisplayName = "Role Display Name 2",
                    Description = "Role Description 2"
                });
            });

            Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

            role = JsonConvert.DeserializeObject<RoleApiModel>(patchResponse.Body.AsString());
            Assert.Equal(name, role.Name);
            Assert.Equal("Role Display Name 2", role.DisplayName);
            Assert.Equal("Role Description 2", role.Description);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task TestAddParentRole_SuccessAsync()
        {
            var parentRoleResponse = await _browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = "parent" + Guid.NewGuid().ToString(),
                });
            });

            Assert.Equal(HttpStatusCode.Created, parentRoleResponse.StatusCode);
            var parentRole = JsonConvert.DeserializeObject<RoleApiModel>(parentRoleResponse.Body.AsString());

            var childRoleResponse = await _browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = "child" + Guid.NewGuid().ToString(),
                    ParentRole = parentRole.Id
                });
            });

            Assert.Equal(HttpStatusCode.Created, childRoleResponse.StatusCode);
            var childRole = JsonConvert.DeserializeObject<RoleApiModel>(childRoleResponse.Body.AsString());
            Assert.True(childRole.Id.HasValue);
            Assert.True(childRole.ParentRole.HasValue);

            var rolesResponse = await _browser.Get($"/roles/app/{_securableItem}");
            Assert.Equal(HttpStatusCode.OK, rolesResponse.StatusCode);

            var roles = JsonConvert.DeserializeObject<List<RoleApiModel>>(rolesResponse.Body.AsString());

            Assert.Equal(3, roles.Count);
            var retrievedChildRole = roles.First(r => r.Id == childRole.Id);
            Assert.Equal(parentRole.Id, retrievedChildRole.ParentRole);

            var retrievedParentRole = roles.First(r => r.Id == parentRole.Id);
            Assert.Contains(childRole.Id.Value, retrievedParentRole.ChildRoles);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task TestAddChildRole_SuccessAsync()
        {
            var childRoleResponse = await _browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = "child" + Guid.NewGuid().ToString(),
                });
            });

            Assert.Equal(HttpStatusCode.Created, childRoleResponse.StatusCode);
            var childRole = JsonConvert.DeserializeObject<RoleApiModel>(childRoleResponse.Body.AsString());

            var childRoleResponse2 = await _browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = "child" + Guid.NewGuid().ToString(),
                });
            });

            Assert.Equal(HttpStatusCode.Created, childRoleResponse2.StatusCode);
            var childRole2 = JsonConvert.DeserializeObject<RoleApiModel>(childRoleResponse2.Body.AsString());

            var parentRoleResponse = await _browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = "parent" + Guid.NewGuid().ToString(),
                    ChildRoles = new[] {childRole.Id, childRole2.Id}
                });
            });

            Assert.Equal(HttpStatusCode.Created, parentRoleResponse.StatusCode);
            var parentRole = JsonConvert.DeserializeObject<RoleApiModel>(parentRoleResponse.Body.AsString());
            Assert.Equal(2, parentRole.ChildRoles.Count());
            Assert.Single(parentRole.ChildRoles.Where(r => r == childRole.Id));
            Assert.Single(parentRole.ChildRoles.Where(r => r == childRole2.Id));
        }

        [Fact]
        public async Task TestAddChildRole_BadChildRole_BadRequestAsync()
        {

            var parentRoleResponse = await _browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = "parent" + Guid.NewGuid().ToString(),
                    ChildRoles = new[] { Guid.NewGuid(), Guid.NewGuid() }
                });
            });

            Assert.Equal(HttpStatusCode.BadRequest, parentRoleResponse.StatusCode);
            var error = parentRoleResponse.Body.DeserializeJson<Error>();
            Assert.Equal(2, error.Details.Length);
        }

        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodName]
        [InlineData("E70ABF1E-D827-432F-9DC1-05D83A574527")]
        [InlineData("BB51C27D-1310-413D-980E-FC2A6DEC78CF")]
        public async Task TestAddGetRole_SuccessAsync(string name)
        {
            var postResponse = await _browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = name
                });
            });

            // Get by name
            var getResponse = await _browser.Get($"/roles/app/{_securableItem}/{name}", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.Contains(name, getResponse.Body.AsString());

            // Get by secitem
            getResponse = await _browser.Get($"/roles/app/{_securableItem}", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.Contains(name, getResponse.Body.AsString());
        }

        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodName]
        [InlineData("0BF47710-11CD-4003-BFEB-2FDF3675513F")]
        public async Task TestDeleteRole_SuccessAsync(string name)
        {
            var postResponse = await _browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = name
                });
            });

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            var role = postResponse.Body.DeserializeJson<RoleApiModel>();

            var delete = await _browser.Delete($"/roles/{role.Id}", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

            // Post role again to ensure role can be created again
            postResponse = await _browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = name
                });
            });

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        }

        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodName]
        [InlineData("90431E6A-8E40-43A8-8564-7AEE1524925D")]
        [InlineData("B1A09125-2E01-4F5D-A77B-6C127C4F98BD")]
        public async Task TestGetRoleBySecItem_SuccessAsync(string name)
        {
            var postResponse = await _browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = name + "_1"
                });
            });

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            postResponse = await _browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = name + "_2"
                });
            });

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            var getResponse = await _browser.Get($"/roles/app/{_securableItem}", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            // Both roles must be found.
            Assert.Contains(name + "_1", getResponse.Body.AsString());
            Assert.Contains(name + "_2", getResponse.Body.AsString());
        }

        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodName]
        [InlineData("C5247AA4-0063-4E68-B1E4-55BD5E0E171D")]
        [InlineData("C5247AA4-0063-4E68-B1E4-55BD5E0E172D")]
        public async Task TestAddNewRole_FailAsync(string id)
        {
            await _browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = id,
                    Id = id
                });
            });

            // Repeat
            var postResponse = await _browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = id,
                    Id = id
                });
            });

            Assert.Equal(HttpStatusCode.Conflict, postResponse.StatusCode);
        }

        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodName]
        [InlineData("C5247AA4-0063-4E68-B1E4-55BD5E0E977D")]
        [InlineData("C5247AA4-0063-4E68-B1E4-55BD5E0E877D")]
        public async Task TestDeleteRole_FailAsync(string id)
        {
            var delete = await _browser.Delete($"/roles/{id}", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task Test_DeletePermissionFromRole_PermissionDoesntExist_NotFoundExceptionAsync()
        {
            var roleName = "Role1" + Guid.NewGuid();

            var postResponse = await _browser.Post("/roles", with =>
                {
                    with.HttpRequest();
                    with.JsonBody(new
                    {
                        Grain = "app",
                        SecurableItem = _securableItem,
                        Name = roleName
                    });
                });

            var getResponse = await _browser.Get($"/roles/app/{_securableItem}/{roleName}", with =>
                {
                    with.HttpRequest();
                });

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var roleApiModelResponse = JsonConvert.DeserializeObject<List<RoleApiModel>>(getResponse.Body.AsString()).First();

            Assert.Equal(roleName, roleApiModelResponse.Name);

            var permissionToDelete = new List<PermissionApiModel>
                                         {
                                             new PermissionApiModel
                                                 {
                                                     Id = Guid.NewGuid(),
                                                     Name = "fakePermission",
                                                     SecurableItem = "fakeApiEndpoint",
                                                     Grain = "app"
                                                 },
                                             new PermissionApiModel
                                                 {
                                                     Id = Guid.NewGuid(),
                                                     Name = "fakePermission2",
                                                     SecurableItem = "fakeApiEndpoint",
                                                     Grain = "app"
                                                 }
                                         };

            var delete = await _browser.Delete($"/roles/{roleApiModelResponse.Id}/permissions", with =>
                {
                    with.HttpRequest();
                    with.JsonBody(permissionToDelete);
                });

            Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task Test_AddPermissionToRole_PermissionDoesntExist_NotFoundExceptionAsync()
        {
            var roleName = "Role1" + Guid.NewGuid();

            var postResponse = await _browser.Post("/roles", with =>
                {
                    with.HttpRequest();
                    with.JsonBody(new
                    {
                        Grain = "app",
                        SecurableItem = _securableItem,
                        Name = roleName
                    });
                });

            var getResponse = await _browser.Get($"/roles/app/{_securableItem}/{roleName}", with =>
                {
                    with.HttpRequest();
                });

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var roleApiModelResponse = JsonConvert.DeserializeObject<List<RoleApiModel>>(getResponse.Body.AsString()).First();

            Assert.Equal(roleName, roleApiModelResponse.Name);

            var permissionToDelete = new List<PermissionApiModel>
                                         {
                                             new PermissionApiModel
                                                 {
                                                     Id = Guid.NewGuid(),
                                                     Name = "fakePermission",
                                                     SecurableItem = "fakeApiEndpoint",
                                                     Grain = "app"
                                                 },
                                             new PermissionApiModel
                                                 {
                                                     Id = Guid.NewGuid(),
                                                     Name = "fakePermission2",
                                                     SecurableItem = "fakeApiEndpoint",
                                                     Grain = "app"
                                                 }
                                         };

            var delete = await _browser.Post($"/roles/{roleApiModelResponse.Id}/permissions", with =>
                {
                    with.HttpRequest();
                    with.JsonBody(permissionToDelete);
                });

            Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task Test_AddPermissionToRole_NoPermissionInBody_BadRequestExceptionAsync()
        {
            var roleName = "Role1" + Guid.NewGuid();

            var postResponse = await _browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = roleName
                });
            });

            var getResponse = await _browser.Get($"/roles/app/{_securableItem}/{roleName}", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var roleApiModelResponse = JsonConvert.DeserializeObject<List<RoleApiModel>>(getResponse.Body.AsString()).First();

            Assert.Equal(roleName, roleApiModelResponse.Name);

            var emptyPermissionArray = new List<PermissionApiModel>();

            var addResponse = await _browser.Post($"/roles/{roleApiModelResponse.Id}/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(emptyPermissionArray);
            });

            Assert.Equal(HttpStatusCode.BadRequest, addResponse.StatusCode);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task Test_AddPermissionToRole_NoIdOnPermission_BadRequestExceptionAsync()
        {
            var roleName = "Role1" + Guid.NewGuid();

            var postResponse = await _browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = roleName
                });
            });

            var getResponse = await _browser.Get($"/roles/app/{_securableItem}/{roleName}", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var roleApiModelResponse = JsonConvert.DeserializeObject<List<RoleApiModel>>(getResponse.Body.AsString()).First();

            Assert.Equal(roleName, roleApiModelResponse.Name);

            var permissionToDelete = new List<PermissionApiModel>
                                         {
                                             new PermissionApiModel
                                                 {                                                     
                                                     Name = "fakePermission",
                                                     SecurableItem = "fakeApiEndpoint",
                                                     Grain = "app"
                                                 }
                                         };

            postResponse = await _browser.Post($"/roles/{roleApiModelResponse.Id}/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(permissionToDelete);
            });

            Assert.Equal(HttpStatusCode.BadRequest, postResponse.StatusCode);
            Assert.Contains(
                "Permission id is required but missing in the request, ensure each permission has an id",
                postResponse.Body.AsString());
        }
    }
}