using System;
using System.Collections.Generic;
using System.Security.Claims;
using Fabric.Authorization.API;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Modules;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Domain.Stores.CouchDB;
using Fabric.Authorization.Domain.Stores.Services;
using Nancy;
using Nancy.Testing;
using Xunit;

namespace Fabric.Authorization.IntegrationTests
{
    [Collection("InMemoryTests")]
    public class PermissionsTests : IntegrationTestsFixture
    {
        public PermissionsTests(bool useInMemoryDB = true)
        {
            var permissionStore = useInMemoryDB ? new InMemoryPermissionStore() : (IPermissionStore)new CouchDbPermissionStore(this.DbService(), this.Logger, this.EventContextResolverService);
            var clientStore = useInMemoryDB ? new InMemoryClientStore() : (IClientStore)new CouchDbClientStore(this.DbService(), this.Logger, this.EventContextResolverService);
            var roleStore = useInMemoryDB ? new InMemoryRoleStore() : (IRoleStore)new CouchDbRoleStore(this.DbService(), this.Logger, this.EventContextResolverService);

            var roleService = new RoleService(roleStore, permissionStore);
            var permissionService = new PermissionService(permissionStore, roleService);
            var clientService = new ClientService(clientStore);

            this.Browser = new Browser(with =>
            {
                with.Module(new PermissionsModule(
                        permissionService,
                        clientService,
                        new Domain.Validators.PermissionValidator(permissionService),
                        this.Logger));
                with.Module(new ClientsModule(
                        clientService,
                        new Domain.Validators.ClientValidator(clientService),
                        this.Logger));
                with.RequestStartup((_, pipelines, context) =>
                {
                    context.CurrentUser = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                    {
                        new Claim(Claims.Scope, Scopes.ManageClientsScope),
                        new Claim(Claims.Scope, Scopes.ReadScope),
                        new Claim(Claims.Scope, Scopes.WriteScope),
                        new Claim(Claims.ClientId, "permissionprincipal"),
                    }, "permissionprincipal"));
                    pipelines.BeforeRequest += (ctx) => RequestHooks.SetDefaultVersionInUrl(ctx);
                });
            }, withDefaults => withDefaults.HostName("testhost"));

            this.Browser.Post("/clients", with =>
            {
                with.HttpRequest();
                with.FormValue("Id", "permissionprincipal");
                with.FormValue("Name", "permissionprincipal");
                with.Header("Accept", "application/json");
            }).Wait();

            Console.WriteLine($"Executing PermissionTests with InMemory: {useInMemoryDB}");
        }

        [Theory]
        [InlineData("InexistentPermission")]
        [InlineData("InexistentPermission2")]
        public void TestGetPermission_Fail(string permission)
        {
            var get = this.Browser.Get($"/permissions/app/permissionprincipal/{permission}", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.OK, get.StatusCode); //TODO: Should be OK or NotFound?
            Assert.True(!get.Body.AsString().Contains(permission));
        }

        [Theory]
        [InlineData("Perm1")]
        [InlineData("Perm2")]
        public void TestAddNewPermission_Success(string permission)
        {
            var postResponse = this.Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "permissionprincipal");
                with.FormValue("Name", permission);
            }).Result;

            // Get by name
            var getResponse = this.Browser.Get($"/permissions/app/permissionprincipal/{permission}", with =>
                {
                    with.HttpRequest();
                    with.Header("Accept", "application/json");
                }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.True(getResponse.Body.AsString().Contains(permission));
        }

        [Theory]
        [InlineData("NewPerm1")]
        [InlineData("NewPerm2")]
        public void TestGetPermission_Success(string permission)
        {
            var postResponse = this.Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "permissionprincipal");
                with.FormValue("Name", permission);
            }).Result;

            // Get by name
            var getResponse = this.Browser.Get($"/permissions/app/permissionprincipal/{permission}", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.True(getResponse.Body.AsString().Contains(permission));

            // Get by secitem
            getResponse = this.Browser.Get($"/permissions/app/permissionprincipal", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.True(getResponse.Body.AsString().Contains(permission));
        }

        [Theory]
        [InlineData("SecItemPerm1")]
        [InlineData("SecItemPerm2")]
        public void TestGetPermissionForSecItem_Success(string permission)
        {
            var postResponse = this.Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "permissionprincipal");
                with.FormValue("Name", permission + "_1");
            }).Result;

            postResponse = this.Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "permissionprincipal");
                with.FormValue("Name", permission + "_2");
            }).Result;

            // Get by secitem
            var getResponse = this.Browser.Get($"/permissions/app/permissionprincipal", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            Assert.True(getResponse.Body.AsString().Contains(permission + "_1"));
            Assert.True(getResponse.Body.AsString().Contains(permission + "_2"));
        }

        [Theory]
        [InlineData("RepeatedPermission1")]
        [InlineData("RepeatedPermission2")]
        public void TestAddNewPermission_Fail(string permission)
        {
            var validPostResponse = this.Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "permissionprincipal");
                with.FormValue("Name", permission);
            }).Result;

            Assert.Equal(HttpStatusCode.Created, validPostResponse.StatusCode);

            // Repeat
            var postResponse = this.Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "permissionprincipal");
                with.FormValue("Name", permission);
            }).Result;

            Assert.Equal(HttpStatusCode.BadRequest, postResponse.StatusCode);
        }

        [Theory]
        [InlineData("PermissionToBeDeleted")]
        [InlineData("PermissionToBeDeleted2")]
        public void TestDeletePermission_Success(string permission)
        {
            var id = Guid.NewGuid();

            this.Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Id", id.ToString());
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "permissionprincipal");
                with.FormValue("Name", permission);
            }).Wait();

            var delete = this.Browser.Delete($"/permissions/{id.ToString()}", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        }

        [Theory]
        [InlineData("18F06565-AA9E-4315-AF27-CEFC165B20FA")]
        [InlineData("18F06565-AA9E-4315-AF27-CEFC165B20FB")]
        public void TestDeletePermission_Fail(string permission)
        {
            var delete = this.Browser.Delete($"/permissions/{permission}", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        }
    }
}