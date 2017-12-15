using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.Domain.Stores;
using Nancy;
using Nancy.Testing;

using Newtonsoft.Json;

using Xunit;

namespace Fabric.Authorization.IntegrationTests.Modules
{
    [Collection("InMemoryTests")]
    public class RolesTests : IntegrationTestsFixture
    {
        public RolesTests(bool useInMemoryDB = true)
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(Claims.Scope, Scopes.ManageClientsScope),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.ClientId, "rolesprincipal")
            }, "rolesprincipal"));

            Browser = GetBrowser(principal, useInMemoryDB);

            Browser.Post("/clients", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Id = "rolesprincipal",
                    Name = "rolesprincipal"
                });
            }).Wait();
        }

        [Theory]
        [DisplayTestMethodName]
        [InlineData("C5247AA4-0063-4E68-B1E4-55BD5E0E177D")]
        [InlineData("C5247AA4-0063-4E68-B1E4-55BD5E0E177E")]
        public void TestGetRole_Fail(string name)
        {
            var get = Browser.Get($"/roles/app/rolesprincipal/{name}", with =>
            {
                with.HttpRequest();
            }).Result;

            Assert.Equal(HttpStatusCode.OK, get.StatusCode); //TODO: Should it be NotFound?
            Assert.True(!get.Body.AsString().Contains(name));
        }

        [Theory]
        [DisplayTestMethodName]
        [InlineData("Role1")]
        [InlineData("Role2")]
        [InlineData("6BC32347-36A1-44CF-AA0E-6C1038AA1DF3")]
        public void TestAddNewRole_Success(string name)
        {
            var postResponse = Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = "rolesprincipal",
                    Name = name
                });
            }).Result;

            var getResponse = Browser.Get($"/roles/app/rolesprincipal/{name}", with =>
            {
                with.HttpRequest();
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.Contains(name, getResponse.Body.AsString());
        }

        [Theory]
        [DisplayTestMethodName]
        [InlineData("NewRole1")]
        [InlineData("NewRole2")]
        public void TestAddGetRole_Success(string name)
        {
            var postResponse = Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = "rolesprincipal",
                    Name = name
                });
            }).Result;

            // Get by name
            var getResponse = Browser.Get($"/roles/app/rolesprincipal/{name}", with =>
            {
                with.HttpRequest();
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.Contains(name, getResponse.Body.AsString());

            // Get by secitem
            getResponse = Browser.Get($"/roles/app/rolesprincipal", with =>
            {
                with.HttpRequest();
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.Contains(name, getResponse.Body.AsString());
        }

        [Theory]
        [DisplayTestMethodName]
        [InlineData("SecItemRole1")]
        [InlineData("SecItemRole2")]
        public void TestGetRoleBySecItem_Success(string name)
        {
            var postResponse = Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = "rolesprincipal",
                    Name = name + "_1"
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            postResponse = Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = "rolesprincipal",
                    Name = name + "_2"
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            var getResponse = Browser.Get($"/roles/app/rolesprincipal", with =>
            {
                with.HttpRequest();
            }).Result;

            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            // Both roles must be found.
            Assert.Contains(name + "_1", getResponse.Body.AsString());
            Assert.Contains(name + "_2", getResponse.Body.AsString());
        }

        [Theory]
        [DisplayTestMethodName]
        [InlineData("C5247AA4-0063-4E68-B1E4-55BD5E0E171D")]
        [InlineData("C5247AA4-0063-4E68-B1E4-55BD5E0E172D")]
        public void TestAddNewRole_Fail(string id)
        {
            Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = "rolesprincipal",
                    Name = id,
                    Id = id
                });
            }).Wait();

            // Repeat
            var postResponse = Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = "rolesprincipal",
                    Name = id,
                    Id = id
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Conflict, postResponse.StatusCode);
        }

        [Theory]
        [DisplayTestMethodName]
        [InlineData("C5247AA4-0063-4E68-B1E4-55BD5E0E977D")]
        [InlineData("C5247AA4-0063-4E68-B1E4-55BD5E0E877D")]
        public void TestDeleteRole_Fail(string id)
        {
            var delete = Browser.Delete($"/roles/{id}", with =>
            {
                with.HttpRequest();
            }).Result;

            Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        }

        [Fact]
        public void Test_DeletePermissionFromRole_PermissionDoesntExist_NotFoundException()
        {
            var roleName = "Role1";

            var postResponse = Browser.Post("/roles", with =>
                {
                    with.HttpRequest();
                    with.JsonBody(new
                    {
                        Grain = "app",
                        SecurableItem = "rolesprincipal",
                        Name = roleName
                    });
                }).Result;

            var getResponse = Browser.Get($"/roles/app/rolesprincipal/{roleName}", with =>
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

            var delete = Browser.Delete($"/roles/{roleApiModelResponse.Id}/permissions", with =>
                {
                    with.HttpRequest();
                    with.JsonBody(permissionToDelete);
                }).Result;

            Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        }

        [Fact]
        public void Test_AddPermissionToRole_PermissionDoesntExist_NotFoundException()
        {
            var roleName = "Role1";

            var postResponse = Browser.Post("/roles", with =>
                {
                    with.HttpRequest();
                    with.JsonBody(new
                    {
                        Grain = "app",
                        SecurableItem = "rolesprincipal",
                        Name = roleName
                    });
                }).Result;

            var getResponse = Browser.Get($"/roles/app/rolesprincipal/{roleName}", with =>
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

            var delete = Browser.Post($"/roles/{roleApiModelResponse.Id}/permissions", with =>
                {
                    with.HttpRequest();
                    with.JsonBody(permissionToDelete);
                }).Result;

            Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        }

        [Fact]
        public void Test_AddPermissionToRole_NoPermissionInBody_BadRequestException()
        {
            var roleName = "Role1";

            var postResponse = Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = "rolesprincipal",
                    Name = roleName
                });
            }).Result;

            var getResponse = Browser.Get($"/roles/app/rolesprincipal/{roleName}", with =>
            {
                with.HttpRequest();
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var roleApiModelResponse = JsonConvert.DeserializeObject<List<RoleApiModel>>(getResponse.Body.AsString()).First();

            Assert.Equal(roleName, roleApiModelResponse.Name);

            var emptyPermissionArray = new List<PermissionApiModel>();

            var delete = Browser.Post($"/roles/{roleApiModelResponse.Id}/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(emptyPermissionArray);
            }).Result;

            Assert.Equal(HttpStatusCode.BadRequest, delete.StatusCode);
        }

        [Fact]
        public void Test_AddPermissionToRole_NoIdOnPermission_BadRequestException()
        {
            var roleName = "Role1";

            var postResponse = Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = "rolesprincipal",
                    Name = roleName
                });
            }).Result;

            var getResponse = Browser.Get($"/roles/app/rolesprincipal/{roleName}", with =>
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

            postResponse = Browser.Post($"/roles/{roleApiModelResponse.Id}/permissions", with =>
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