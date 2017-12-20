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
    [Collection("InMemoryTests")]
    public class RolesTests : IClassFixture<IntegrationTestsFixture>
    {
        private readonly Browser _browser;
        private readonly string _securableItem;
        private readonly string _subjectId;
        public RolesTests(IntegrationTestsFixture fixture, bool useInMemoryDb = true)
        {
            _securableItem = "rolesprincipal" + Guid.NewGuid();
            _subjectId = _securableItem;
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(Claims.Scope, Scopes.ManageClientsScope),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.ClientId, _securableItem)
            }, _securableItem));

            _browser = fixture.GetBrowser(principal, useInMemoryDb);
            fixture.CreateClient(_browser, _securableItem);
        }

        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodName]
        [InlineData("C5247AA4-0063-4E68-B1E4-55BD5E0E177D")]
        [InlineData("C5247AA4-0063-4E68-B1E4-55BD5E0E177E")]
        public void TestGetRole_Fail(string name)
        {
            var get = _browser.Get($"/roles/app/{_securableItem}/{name}", with =>
            {
                with.HttpRequest();
            }).Result;

            Assert.Equal(HttpStatusCode.OK, get.StatusCode); //TODO: Should it be NotFound?
            Assert.True(!get.Body.AsString().Contains(name));
        }

        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodName]
        [InlineData("EA318378-CCA3-42B4-93E2-F2FBF12E679A")]
        [InlineData("2374EEB4-EC72-454D-915B-23A89AD67879")]
        [InlineData("6BC32347-36A1-44CF-AA0E-6C1038AA1DF3")]
        public void TestAddNewRole_Success(string name)
        {
            var postResponse = _browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = name
                });
            }).Result;

            var getResponse = _browser.Get($"/roles/app/{_securableItem}/{name}", with =>
            {
                with.HttpRequest();
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.Contains(name, getResponse.Body.AsString());
        }

        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodName]
        [InlineData("E70ABF1E-D827-432F-9DC1-05D83A574527")]
        [InlineData("BB51C27D-1310-413D-980E-FC2A6DEC78CF")]
        public void TestAddGetRole_Success(string name)
        {
            var postResponse = _browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = name
                });
            }).Result;

            // Get by name
            var getResponse = _browser.Get($"/roles/app/{_securableItem}/{name}", with =>
            {
                with.HttpRequest();
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.Contains(name, getResponse.Body.AsString());

            // Get by secitem
            getResponse = _browser.Get($"/roles/app/{_securableItem}", with =>
            {
                with.HttpRequest();
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.Contains(name, getResponse.Body.AsString());
        }

        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodName]
        [InlineData("90431E6A-8E40-43A8-8564-7AEE1524925D")]
        [InlineData("B1A09125-2E01-4F5D-A77B-6C127C4F98BD")]
        public void TestGetRoleBySecItem_Success(string name)
        {
            var postResponse = _browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = name + "_1"
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            postResponse = _browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = name + "_2"
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            var getResponse = _browser.Get($"/roles/app/{_securableItem}", with =>
            {
                with.HttpRequest();
            }).Result;

            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            // Both roles must be found.
            Assert.Contains(name + "_1", getResponse.Body.AsString());
            Assert.Contains(name + "_2", getResponse.Body.AsString());
        }

        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodName]
        [InlineData("C5247AA4-0063-4E68-B1E4-55BD5E0E171D")]
        [InlineData("C5247AA4-0063-4E68-B1E4-55BD5E0E172D")]
        public void TestAddNewRole_Fail(string id)
        {
            _browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = id,
                    Id = id
                });
            }).Wait();

            // Repeat
            var postResponse = _browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = id,
                    Id = id
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Conflict, postResponse.StatusCode);
        }

        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodName]
        [InlineData("C5247AA4-0063-4E68-B1E4-55BD5E0E977D")]
        [InlineData("C5247AA4-0063-4E68-B1E4-55BD5E0E877D")]
        public void TestDeleteRole_Fail(string id)
        {
            var delete = _browser.Delete($"/roles/{id}", with =>
            {
                with.HttpRequest();
            }).Result;

            Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        }

        [Fact]
        public void Test_DeletePermissionFromRole_PermissionDoesntExist_NotFoundException()
        {
            var roleName = "Role1" + Guid.NewGuid();

            var postResponse = _browser.Post("/roles", with =>
                {
                    with.HttpRequest();
                    with.JsonBody(new
                    {
                        Grain = "app",
                        SecurableItem = _securableItem,
                        Name = roleName
                    });
                }).Result;

            var getResponse = _browser.Get($"/roles/app/{_securableItem}/{roleName}", with =>
                {
                    with.HttpRequest();
                }).Result;

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

            var delete = _browser.Delete($"/roles/{roleApiModelResponse.Id}/permissions", with =>
                {
                    with.HttpRequest();
                    with.JsonBody(permissionToDelete);
                }).Result;

            Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        }

        [Fact]
        public void Test_AddPermissionToRole_PermissionDoesntExist_NotFoundException()
        {
            var roleName = "Role1" + Guid.NewGuid();

            var postResponse = _browser.Post("/roles", with =>
                {
                    with.HttpRequest();
                    with.JsonBody(new
                    {
                        Grain = "app",
                        SecurableItem = _securableItem,
                        Name = roleName
                    });
                }).Result;

            var getResponse = _browser.Get($"/roles/app/{_securableItem}/{roleName}", with =>
                {
                    with.HttpRequest();
                }).Result;

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

            var delete = _browser.Post($"/roles/{roleApiModelResponse.Id}/permissions", with =>
                {
                    with.HttpRequest();
                    with.JsonBody(permissionToDelete);
                }).Result;

            Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        }

        [Fact]
        public void Test_AddPermissionToRole_NoPermissionInBody_BadRequestException()
        {
            var roleName = "Role1" + Guid.NewGuid();

            var postResponse = _browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = roleName
                });
            }).Result;

            var getResponse = _browser.Get($"/roles/app/{_securableItem}/{roleName}", with =>
            {
                with.HttpRequest();
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var roleApiModelResponse = JsonConvert.DeserializeObject<List<RoleApiModel>>(getResponse.Body.AsString()).First();

            Assert.Equal(roleName, roleApiModelResponse.Name);

            var emptyPermissionArray = new List<PermissionApiModel>();

            var addResponse = _browser.Post($"/roles/{roleApiModelResponse.Id}/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(emptyPermissionArray);
            }).Result;

            Assert.Equal(HttpStatusCode.BadRequest, addResponse.StatusCode);
        }

        [Fact]
        public void Test_AddPermissionToRole_NoIdOnPermission_BadRequestException()
        {
            var roleName = "Role1" + Guid.NewGuid();

            var postResponse = _browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = roleName
                });
            }).Result;

            var getResponse = _browser.Get($"/roles/app/{_securableItem}/{roleName}", with =>
            {
                with.HttpRequest();
            }).Result;

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

            postResponse = _browser.Post($"/roles/{roleApiModelResponse.Id}/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(permissionToDelete);
            }).Result;

            Assert.Equal(HttpStatusCode.BadRequest, postResponse.StatusCode);
            Assert.Contains(
                "Permission id is required but missing in the request, ensure each permission has an id",
                postResponse.Body.AsString());
        }
    }
}