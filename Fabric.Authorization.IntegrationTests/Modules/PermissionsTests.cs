using System.Collections.Generic;
using System.Security.Claims;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Modules;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores;
using Moq;
using Nancy;
using Nancy.Testing;
using Serilog;
using Xunit;

namespace Fabric.Authorization.IntegrationTests
{
    public class PermissionsTests : IntegrationTestsFixture
    {
        public PermissionsTests()
        {
            var store = new InMemoryPermissionStore();
            var permissionService = new PermissionService(store);
            var clientService = new ClientService(new InMemoryClientStore());

            this.Browser = new Browser(with =>
            {
                with.Module(new PermissionsModule(
                        permissionService,
                        clientService,
                        new Domain.Validators.PermissionValidator(store),
                        new Mock<ILogger>().Object));
                with.RequestStartup((_, __, context) =>
                {
                    context.CurrentUser = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>()
                    {
                        new Claim(Claims.Scope, Scopes.ManageClientsScope),
                        new Claim(Claims.Scope, Scopes.ReadScope),
                        new Claim(Claims.Scope, Scopes.WriteScope),
                    }, "testprincipal"));
                });
            });
        }

        //var values = new Dictionary<string, string>() { };

        [Theory]
        [InlineData("InexistentPermission")]
        [InlineData("InexistentPermission2")]
        public void TestGetPermission_Fail(string PermissionName)
        {
            var get = this.Browser.Get($"/permissions/{PermissionName}", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        }

        [Theory]
        [InlineData("Permission1")]
        [InlineData("Permission2")]
        [InlineData("6BC32347-36A1-44CF-AA0E-6C1038AA1DF3")]
        public void TestAddNewPermission_Success(string PermissionName)
        {
            var postResponse = this.Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.FormValue("Grain", PermissionName);
                with.FormValue("SecurableItem", PermissionName);
                with.FormValue("Name", PermissionName);
            }).Result;

            var getResponse = this.Browser.Get($"/permissions/{PermissionName}", with =>
                {
                    with.HttpRequest();
                    with.Header("Accept", "application/json");
                }).Result;

            Assert.Equal(HttpStatusCode.NoContent, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.True(getResponse.Body.AsString().Contains(PermissionName));
        }

        [Theory]
        [InlineData("RepeatedPermission1")]
        [InlineData("RepeatedPermission2")]
        public void TestAddNewPermission_Fail(string PermissionName)
        {
            this.Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.FormValue("Grain", PermissionName);
                with.FormValue("SecurableItem", PermissionName);
                with.FormValue("Name", PermissionName);
            }).Wait();

            // Repeat
            var postResponse = this.Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.FormValue("PermissionName", PermissionName);
            }).Result;

            Assert.Equal(HttpStatusCode.BadRequest, postResponse.StatusCode);
        }

        [Theory]
        [InlineData("PermissionToBeDeleted")]
        [InlineData("PermissionToBeDeleted2")]
        public void TestDeletePermission_Success(string PermissionName)
        {
            this.Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.FormValue("Grain", PermissionName);
                with.FormValue("SecurableItem", PermissionName);
                with.FormValue("Name", PermissionName);
            }).Wait();

            var delete = this.Browser.Delete($"/permissions/{PermissionName}", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        }

        [Theory]
        [InlineData("InexistentPermission")]
        [InlineData("InexistentPermission2")]
        public void TestDeletePermission_Fail(string PermissionName)
        {
            var delete = this.Browser.Delete($"/permissions/{PermissionName}", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        }
    }
}