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
                        new Claim(Claims.ClientId, "rolesPrincipal"),
                    }, "rolesPrincipal"));
                });
            });

            this.Browser.Post("/clients", with =>
            {
                with.HttpRequest();
                with.FormValue("Id", "rolesPrincipal");
                with.FormValue("Name", "rolesPrincipal");
                with.Header("Accept", "application/json");
            }).Wait();
        }

        [Theory]
        [InlineData("C5247AA4-0063-4E68-B1E4-55BD5E0E177D")]
        [InlineData("C5247AA4-0063-4E68-B1E4-55BD5E0E177E")]
        public void TestGetRole_Fail(string name)
        {
            var get = this.Browser.Get($"/roles/app/rolesPrincipal/{name}", with =>
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
                with.FormValue("SecurableItem", "rolesPrincipal");
                with.FormValue("Name", name);
            }).Result;

            var getResponse = this.Browser.Get($"/roles/app/rolesPrincipal/{name}", with =>
                {
                    with.HttpRequest();
                    with.Header("Accept", "application/json");
                }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.True(getResponse.Body.AsString().Contains(name));
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
                with.FormValue("SecurableItem", "rolesPrincipal");
                with.FormValue("Name", id);
                with.FormValue("Id", id);
            }).Wait();

            // Repeat
            var postResponse = this.Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "rolesPrincipal");
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