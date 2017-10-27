using System;
using System.Collections.Generic;
using System.Security.Claims;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Infrastructure.PipelineHooks;
using Fabric.Authorization.API.Modules;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Domain.Stores.CouchDB;
using Fabric.Authorization.Domain.Stores.InMemory;
using Fabric.Authorization.Domain.Stores.Services;
using Fabric.Authorization.Domain.Validators;
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
            var permissionStore = useInMemoryDB
                ? new InMemoryPermissionStore(_identifierFormatter)
                : (IPermissionStore) new CouchDbPermissionStore(DbService(), Logger, EventContextResolverService, new IdpIdentifierFormatter());

            var clientStore = useInMemoryDB
                ? new InMemoryClientStore()
                : (IClientStore) new CouchDbClientStore(DbService(), Logger, EventContextResolverService);

            var roleStore = useInMemoryDB
                ? new InMemoryRoleStore()
                : (IRoleStore) new CouchDbRoleStore(DbService(), Logger, EventContextResolverService);

            var clientService = new ClientService(clientStore);
            var roleService = new RoleService(roleStore, permissionStore, clientService);
            var permissionService = new PermissionService(permissionStore, roleService);

            Browser = new Browser(with =>
            {
                with.Module(new PermissionsModule(
                    permissionService,
                    clientService,
                    new PermissionValidator(permissionService),
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
                        new Claim(Claims.ClientId, "permissionprincipal")
                    }, "permissionprincipal"));
                    pipelines.BeforeRequest += ctx => RequestHooks.SetDefaultVersionInUrl(ctx);
                });
            }, withDefaults => withDefaults.HostName("testhost"));

            Browser.Post("/clients", with =>
            {
                with.HttpRequest();
                with.FormValue("Id", "permissionprincipal");
                with.FormValue("Name", "permissionprincipal");
                with.Header("Accept", "application/json");
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
                with.Header("Accept", "application/json");
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
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "permissionprincipal");
                with.FormValue("Name", permission);
            }).Result;

            // Get by name
            var getResponse = Browser.Get($"/permissions/app/permissionprincipal/{permission}", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.True(getResponse.Body.AsString().Contains(permission));
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
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "permissionprincipal");
                with.FormValue("Name", permission);
            }).Result;

            // Get by name
            var getResponse = Browser.Get($"/permissions/app/permissionprincipal/{permission}", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.True(getResponse.Body.AsString().Contains(permission));

            // Get by secitem
            getResponse = Browser.Get("/permissions/app/permissionprincipal", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.True(getResponse.Body.AsString().Contains(permission));
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
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "permissionprincipal");
                with.FormValue("Name", permission + "_1");
            }).Result;

            postResponse = Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "permissionprincipal");
                with.FormValue("Name", permission + "_2");
            }).Result;

            // Get by secitem
            var getResponse = Browser.Get($"/permissions/app/permissionprincipal", with =>
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
        [DisplayTestMethodName]
        [InlineData("RepeatedPermission1")]
        [InlineData("RepeatedPermission2")]
        public void TestAddNewPermission_Fail(string permission)
        {
            var validPostResponse = Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "permissionprincipal");
                with.FormValue("Name", permission);
            }).Result;

            Assert.Equal(HttpStatusCode.Created, validPostResponse.StatusCode);

            // Repeat
            var postResponse = Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "permissionprincipal");
                with.FormValue("Name", permission);
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
                with.Header("Accept", "application/json");
                with.FormValue("Id", id.ToString());
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "permissionprincipal");
                with.FormValue("Name", permission);
            }).Wait();

            var delete = Browser.Delete($"/permissions/{id}", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
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
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        }
    }
}