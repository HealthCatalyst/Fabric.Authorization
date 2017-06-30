using System.Collections.Generic;
using System.Security.Claims;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Models;
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
    public class RolesTests : IntegrationTestsFixture
    {
        public RolesTests()
        {
            var store = new InMemoryRoleStore();
            var roleService = new RoleService(store, new InMemoryPermissionStore());
            var clientService = new ClientService(new InMemoryClientStore());

            this.Browser = new Browser(with =>
            {
                with.Module(new RolesModule(
                        roleService,
                        clientService,
                        new Domain.Validators.RoleValidator(store),
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

        [Theory]
        [InlineData("InexistentRole")]
        [InlineData("InexistentRole2")]
        public void TestGetRole_Fail(string id)
        {
            var get = this.Browser.Get($"/Roles/{id}", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        }

        [Theory]
        [InlineData("Role1")]
        [InlineData("Role2")]
        [InlineData("6BC32347-36A1-44CF-AA0E-6C1038AA1DF3")]
        public void TestAddNewRole_Success(string id)
        {
            var postResponse = this.Browser.Post("/Roles", with =>
            {
                with.HttpRequest();
                with.FormValue("Grain", id);
                with.FormValue("SecurableItem", id);
                with.FormValue("Name", id);

            }).Result;

            var getResponse = this.Browser.Get($"/Roles/{id}", with =>
                {
                    with.HttpRequest();
                    with.Header("Accept", "application/json");
                }).Result;

            Assert.Equal(HttpStatusCode.NoContent, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.True(getResponse.Body.AsString().Contains(id));
        }
        
        [Theory]
        [InlineData("RepeatedRole1")]
        [InlineData("RepeatedRole2")]
        public void TestAddNewRole_Fail(string id)
        {
            this.Browser.Post("/Roles", with =>
            {
                with.HttpRequest();
                with.FormValue("Grain", id);
                with.FormValue("SecurableItem", id);
                with.FormValue("Name", id);
            }).Wait();

            // Repeat
            var postResponse = this.Browser.Post("/Roles", with =>
            {
                with.HttpRequest();
                with.FormValue("id", id);
            }).Result;

            Assert.Equal(HttpStatusCode.BadRequest, postResponse.StatusCode);
        }

        [Theory]
        [InlineData("RoleToBeDeleted")]
        [InlineData("RoleToBeDeleted2")]
        public void TestDeleteRole_Success(string id)
        {
            this.Browser.Post("/Roles", with =>
            {
                with.HttpRequest();
                with.FormValue("Grain", id);
                with.FormValue("SecurableItem", id);
                with.FormValue("Name", id);
            }).Wait();

            var delete = this.Browser.Delete($"/Roles/{id}", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        }

        [Theory]
        [InlineData("InexistentRole")]
        [InlineData("InexistentRole2")]
        public void TestDeleteRole_Fail(string id)
        {
            var delete = this.Browser.Delete($"/Roles/{id}", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        }
    }
}