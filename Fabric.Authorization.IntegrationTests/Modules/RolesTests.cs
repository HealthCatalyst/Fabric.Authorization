using System;
using System.Collections.Generic;
using System.Security.Claims;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Modules;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores;
using Nancy;
using Nancy.Testing;
using Xunit;

namespace Fabric.Authorization.IntegrationTests
{
    [Collection("InMemoryTests")]
    public class RolesTests : IntegrationTestsFixture
    {
        public RolesTests(bool useInMemoryDB = true)
        {
            var store = useInMemoryDB ? new InMemoryRoleStore() : (IRoleStore)new CouchDBRoleStore(this.DbService(), this.Logger);
            var clientStore = useInMemoryDB ? new InMemoryClientStore() : (IClientStore)new CouchDBClientStore(this.DbService(), this.Logger);

            var roleService = new RoleService(store, new InMemoryPermissionStore());
            var clientService = new ClientService(clientStore);

            this.Browser = new Browser(with =>
            {
                with.Module(new RolesModule(
                        roleService,
                        clientService,
                        new Domain.Validators.RoleValidator(store),
                        this.Logger));

                with.Module(new ClientsModule(
                        clientService,
                        new Domain.Validators.ClientValidator(clientStore),
                        this.Logger));

                with.RequestStartup((_, __, context) =>
                {
                    context.CurrentUser = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>()
                    {
                        new Claim(Claims.Scope, Scopes.ManageClientsScope),
                        new Claim(Claims.Scope, Scopes.ReadScope),
                        new Claim(Claims.Scope, Scopes.WriteScope),
                        new Claim(Claims.ClientId, "rolesprincipal"),
                    }, "rolesprincipal"));
                });
            });

            this.Browser.Post("/clients", with =>
                {
                    with.HttpRequest();
                    with.FormValue("Id", "rolesprincipal");
                    with.FormValue("Name", "rolesprincipal");
                    with.Header("Accept", "application/json");
                }).Wait();
            
        }

        [Theory]
        [InlineData("C5247AA4-0063-4E68-B1E4-55BD5E0E177D")]
        [InlineData("C5247AA4-0063-4E68-B1E4-55BD5E0E177E")]
        public void TestGetRole_Fail(string name)
        {
            var get = this.Browser.Get($"/roles/app/rolesprincipal/{name}", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.OK, get.StatusCode); //TODO: Should it be NotFound?
            Assert.True(!get.Body.AsString().Contains(name));
        }

        [Theory]
        [InlineData("Role1")]
        [InlineData("Role2")]
        [InlineData("6BC32347-36A1-44CF-AA0E-6C1038AA1DF3")]
        public void TestAddNewRole_Success(string name)
        {
            var postResponse = this.Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "rolesprincipal");
                with.FormValue("Name", name);
            }).Result;

            var getResponse = this.Browser.Get($"/roles/app/rolesprincipal/{name}", with =>
                {
                    with.HttpRequest();
                    with.Header("Accept", "application/json");
                }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.True(getResponse.Body.AsString().Contains(name));
        }

        [Theory]
        [InlineData("NewRole1")]
        [InlineData("NewRole2")]
        public void TestAddGetRole_Success(string name)
        {
            var postResponse = this.Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "rolesprincipal");
                with.FormValue("Name", name);
            }).Result;

            // Get by name
            var getResponse = this.Browser.Get($"/roles/app/rolesprincipal/{name}", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.True(getResponse.Body.AsString().Contains(name));

            // Get by secitem
            getResponse = this.Browser.Get($"/roles/app/rolesprincipal", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.True(getResponse.Body.AsString().Contains(name));
        }

        [Theory]
        [InlineData("SecItemRole1")]
        [InlineData("SecItemRole2")]
        public void TestGetRoleBySecItem_Success(string name)
        {
            var postResponse = this.Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "rolesprincipal");
                with.FormValue("Name", name + "_1");
            }).Result;

            postResponse = this.Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "rolesprincipal");
                with.FormValue("Name", name + "_2");
            }).Result;

            var getResponse = this.Browser.Get($"/roles/app/rolesprincipal", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            // Both roles must be found.
            Assert.True(getResponse.Body.AsString().Contains(name + "_1"));
            Assert.True(getResponse.Body.AsString().Contains(name + "_2"));
        }

        [Theory]
        [InlineData("C5247AA4-0063-4E68-B1E4-55BD5E0E171D")]
        [InlineData("C5247AA4-0063-4E68-B1E4-55BD5E0E172D")]
        public void TestAddNewRole_Fail(string id)
        {
            this.Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "rolesprincipal");
                with.FormValue("Name", id);
                with.FormValue("Id", id);
            }).Wait();

            // Repeat
            var postResponse = this.Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "rolesprincipal");
                with.FormValue("Name", id);
                with.FormValue("Id", id);
            }).Result;

            Assert.Equal(HttpStatusCode.BadRequest, postResponse.StatusCode);
        }

        [Theory]
        [InlineData("C5247AA4-0063-4E68-B1E4-55BD5E0E977D")]
        [InlineData("C5247AA4-0063-4E68-B1E4-55BD5E0E877D")]
        public void TestDeleteRole_Fail(string id)
        {
            var delete = this.Browser.Delete($"/roles/{id}", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        }
    }
}