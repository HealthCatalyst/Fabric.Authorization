using System;
using System.Collections.Generic;
using System.Security.Claims;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.Domain.Stores;
using Nancy;
using Nancy.Testing;
using Xunit;

namespace Fabric.Authorization.IntegrationTests.Modules
{
    [Collection("InMemoryTests")]
    public class PermissionsTests : IntegrationTestsFixture
    {
        private readonly IIdentifierFormatter _identifierFormatter = new IdpIdentifierFormatter();

        public PermissionsTests(bool useInMemoryDB = true)
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(Claims.Scope, Scopes.ManageClientsScope),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.ClientId, "permissionprincipal")
            }, "permissionprincipal"));

            Browser = GetBrowser(principal, useInMemoryDB);

            Browser.Post("/clients", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Id = "permissionprincipal",
                    Name = "permissionprincipal"
                });
            }).Wait();
        }

        [Theory]
        [DisplayTestMethodName]
        [InlineData("InexistentPermission")]
        [InlineData("InexistentPermission2")]
        public void TestGetPermission_Fail(string permission)
        {
            var get = Browser.Get($"/permissions/app/permissionprincipal/{permission}", with =>
            {
                with.HttpRequest();
            }).Result;

            Assert.Equal(HttpStatusCode.OK, get.StatusCode); //TODO: Should be OK or NotFound?
            Assert.True(!get.Body.AsString().Contains(permission));
        }

        [Theory]
        [DisplayTestMethodName]
        [InlineData("Perm1")]
        [InlineData("Perm2")]
        public void TestAddNewPermission_Success(string permission)
        {
            var postResponse = Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = "permissionprincipal",
                    Name = permission
                });
            }).Result;

            // Get by name
            var getResponse = Browser.Get($"/permissions/app/permissionprincipal/{permission}", with =>
            {
                with.HttpRequest();
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.Contains(permission, getResponse.Body.AsString());
        }

        [Theory]
        [DisplayTestMethodName]
        [InlineData("NewPerm1")]
        [InlineData("NewPerm2")]
        public void TestGetPermission_Success(string permission)
        {
            var postResponse = Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = "permissionprincipal",
                    Name = permission
                });
            }).Result;

            // Get by name
            var getResponse = Browser.Get($"/permissions/app/permissionprincipal/{permission}", with =>
            {
                with.HttpRequest();
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.Contains(permission, getResponse.Body.AsString());

            // Get by secitem
            getResponse = Browser.Get("/permissions/app/permissionprincipal", with =>
            {
                with.HttpRequest();
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.Contains(permission, getResponse.Body.AsString());
        }

        [Theory]
        [DisplayTestMethodName]
        [InlineData("SecItemPerm1")]
        [InlineData("SecItemPerm2")]
        public void TestGetPermissionForSecItem_Success(string permission)
        {
            var postResponse = Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = "permissionprincipal",
                    Name = permission + "_1"
                });
            }).Result;

            postResponse = Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = "permissionprincipal",
                    Name = permission + "_2"
                });
            }).Result;

            // Get by secitem
            var getResponse = Browser.Get("/permissions/app/permissionprincipal", with =>
            {
                with.HttpRequest();
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            Assert.Contains(permission + "_1", getResponse.Body.AsString());
            Assert.Contains(permission + "_2", getResponse.Body.AsString());
        }

        [Theory]
        [DisplayTestMethodName]
        [InlineData("RepeatedPermission1")]
        [InlineData("RepeatedPermission2")]
        public void TestAddNewPermission_Fail(string permission)
        {
            var validPostResponse = Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = "permissionprincipal",
                    Name = permission
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Created, validPostResponse.StatusCode);

            // Repeat
            var postResponse = Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = "permissionprincipal",
                    Name = permission
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Conflict, postResponse.StatusCode);
        }

        [Theory]
        [DisplayTestMethodName]
        [InlineData("PermissionToBeDeleted")]
        [InlineData("PermissionToBeDeleted2")]
        public void TestDeletePermission_Success(string permission)
        {
            var id = Guid.NewGuid();

            Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Id = id.ToString(),
                    Grain = "app",
                    SecurableItem = "permissionprincipal",
                    Name = permission
                });
            }).Wait();

            var delete = Browser.Delete($"/permissions/{id}", with =>
            {
                with.HttpRequest();
            }).Result;

            Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        }

        [Theory]
        [DisplayTestMethodName]
        [InlineData("18F06565-AA9E-4315-AF27-CEFC165B20FA")]
        [InlineData("18F06565-AA9E-4315-AF27-CEFC165B20FB")]
        public void TestDeletePermission_Fail(string permission)
        {
            var delete = Browser.Delete($"/permissions/{permission}", with =>
            {
                with.HttpRequest();
            }).Result;

            Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        }
    }
}