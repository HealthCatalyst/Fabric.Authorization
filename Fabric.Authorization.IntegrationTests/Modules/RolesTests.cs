using System.Collections.Generic;
using System.Security.Claims;
using Fabric.Authorization.API;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Modules;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Domain.Stores.CouchDB;
using Fabric.Authorization.Domain.Stores.Services;
using Fabric.Authorization.Domain.Validators;
using Nancy;
using Nancy.Testing;
using Xunit;

namespace Fabric.Authorization.IntegrationTests.Modules
{
    [Collection("InMemoryTests")]
    public class RolesTests : IntegrationTestsFixture
    {
        public RolesTests(bool useInMemoryDB = true)
        {
            var store = useInMemoryDB
                ? new InMemoryRoleStore()
                : (IRoleStore) new CouchDbRoleStore(DbService(), Logger, EventContextResolverService);
            var clientStore = useInMemoryDB
                ? new InMemoryClientStore()
                : (IClientStore) new CouchDbClientStore(DbService(), Logger, EventContextResolverService);

            var roleService = new RoleService(store, new InMemoryPermissionStore());
            var clientService = new ClientService(clientStore);

            Browser = new Browser(with =>
            {
                with.Module(new RolesModule(
                    roleService,
                    clientService,
                    new RoleValidator(roleService),
                    Logger));

                with.Module(new ClientsModule(
                    clientService,
                    new ClientValidator(clientService),
                    Logger));

                with.RequestStartup((_, pipelines, context) =>
                {
                    context.CurrentUser = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                    {
                        new Claim(Claims.Scope, Scopes.ManageClientsScope),
                        new Claim(Claims.Scope, Scopes.ReadScope),
                        new Claim(Claims.Scope, Scopes.WriteScope),
                        new Claim(Claims.ClientId, "rolesprincipal")
                    }, "rolesprincipal"));
                    pipelines.BeforeRequest += ctx => RequestHooks.SetDefaultVersionInUrl(ctx);
                });
            }, withDefaults => withDefaults.HostName("testhost"));

            Browser.Post("/clients", with =>
            {
                with.HttpRequest();
                with.FormValue("Id", "rolesprincipal");
                with.FormValue("Name", "rolesprincipal");
                with.Header("Accept", "application/json");
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
                with.Header("Accept", "application/json");
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
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "rolesprincipal");
                with.FormValue("Name", name);
            }).Result;

            var getResponse = Browser.Get($"/roles/app/rolesprincipal/{name}", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.True(getResponse.Body.AsString().Contains(name));
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
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "rolesprincipal");
                with.FormValue("Name", name);
            }).Result;

            // Get by name
            var getResponse = Browser.Get($"/roles/app/rolesprincipal/{name}", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.True(getResponse.Body.AsString().Contains(name));

            // Get by secitem
            getResponse = Browser.Get($"/roles/app/rolesprincipal", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.True(getResponse.Body.AsString().Contains(name));
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
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "rolesprincipal");
                with.FormValue("Name", name + "_1");
            }).Result;

            postResponse = Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "rolesprincipal");
                with.FormValue("Name", name + "_2");
            }).Result;

            var getResponse = Browser.Get($"/roles/app/rolesprincipal", with =>
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
        [DisplayTestMethodName]
        [InlineData("C5247AA4-0063-4E68-B1E4-55BD5E0E171D")]
        [InlineData("C5247AA4-0063-4E68-B1E4-55BD5E0E172D")]
        public void TestAddNewRole_Fail(string id)
        {
            Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "rolesprincipal");
                with.FormValue("Name", id);
                with.FormValue("Id", id);
            }).Wait();

            // Repeat
            var postResponse = Browser.Post("/roles", with =>
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
        [DisplayTestMethodName]
        [InlineData("C5247AA4-0063-4E68-B1E4-55BD5E0E977D")]
        [InlineData("C5247AA4-0063-4E68-B1E4-55BD5E0E877D")]
        public void TestDeleteRole_Fail(string id)
        {
            var delete = Browser.Delete($"/roles/{id}", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        }
    }
}