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
using Fabric.Authorization.IntegrationTests.Modules;
using Nancy;
using Nancy.Testing;
using Xunit;

namespace Fabric.Authorization.IntegrationTests.CouchDB
{
    [Collection("CouchTests")]
    public class CouchDBGroupsTests : IntegrationTestsFixture
    {
        private readonly IRoleStore _roleStore;
        private readonly IIdentifierFormatter _identifierFormatter = new IdpIdentifierFormatter();

        public CouchDBGroupsTests()
        {
            var useInMemoryDB = false;
            var groupStore = useInMemoryDB
                ? new InMemoryGroupStore(_identifierFormatter)
                : (IGroupStore)new CouchDbGroupStore(DbService(), Logger, EventContextResolverService, _identifierFormatter);

            _roleStore = useInMemoryDB
                ? new InMemoryRoleStore()
                : (IRoleStore)new CouchDbRoleStore(DbService(), Logger, EventContextResolverService);

            var userStore = useInMemoryDB
                ? new InMemoryUserStore(_identifierFormatter)
                : (IUserStore)new CouchDbUserStore(DbService(), Logger, EventContextResolverService, _identifierFormatter);

            var permissionStore = useInMemoryDB
                ? new InMemoryPermissionStore(_identifierFormatter)
                : (IPermissionStore)new CouchDbPermissionStore(DbService(), Logger, EventContextResolverService, _identifierFormatter);

            var clientStore = useInMemoryDB
                ? new InMemoryClientStore()
                : (IClientStore)new CouchDbClientStore(DbService(), Logger, EventContextResolverService);

            var userService = new UserService(userStore);
            var clientService = new ClientService(clientStore);
            var roleService = new RoleService(_roleStore, permissionStore, clientService);
            var groupService = new GroupService(groupStore, _roleStore, userStore, roleService);
            var permissionService = new PermissionService(permissionStore, roleService);

            Browser = new Browser(with =>
            {
                with.Module(new GroupsModule(
                    groupService,
                    new GroupValidator(groupService),
                    Logger,
                    DefaultPropertySettings));

                with.Module(new RolesModule(
                    roleService,
                    clientService,
                    new RoleValidator(roleService),
                    Logger));

                with.Module(new ClientsModule(
                    clientService,
                    new ClientValidator(clientService),
                    Logger));

                with.Module(new UsersModule(
                    clientService,
                    permissionService,
                    userService,
                    roleService,
                    new UserValidator(),
                    Logger));

                with.Module(new PermissionsModule(
                    permissionService,
                    clientService,
                    new PermissionValidator(permissionService),
                    Logger));

                with.RequestStartup((_, pipelines, context) =>
                {
                    context.CurrentUser = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                    {
                        new Claim(Claims.Scope, Scopes.ManageClientsScope),
                        new Claim(Claims.Scope, Scopes.ReadScope),
                        new Claim(Claims.Scope, Scopes.WriteScope),
                        new Claim(Claims.ClientId, "rolesprincipal"),
                        new Claim(Claims.IdentityProvider, "idP1")
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
        [InlineData("GroupToBeDeleted", "Source1")]
        [InlineData("GroupToBeDeleted2", "Source2")]
        public void DeleteGroup_SingleGroup_Success(string groupName, string groupSource)
        {
            Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("GroupName", groupName);
                with.FormValue("GroupSource", groupSource);
                with.Header("Accept", "application/json");
            }).Wait();

            var delete = Browser.Delete($"/groups/{groupName}", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        }

        [Theory]
        [DisplayTestMethodName]
        [InlineData("BatchUpdateGroup1", "BatchUpdateSource1")]
        [InlineData("BatchUpdateGroup2", "BatchUpdateSource2")]
        public void UpdateGroup_Batch_Success(string groupName, string groupSource)
        {
            var postResponse = Browser.Post("/groups/UpdateGroups", with =>
            {
                with.HttpRequest();
                with.FormValue("Id[0]", groupName + "_0");
                with.FormValue("Id[1]", groupName + "_1");
                with.FormValue("Id[2]", groupName + "_2");

                with.FormValue("GroupName[0]", groupName + "_0");
                with.FormValue("GroupName[1]", groupName + "_1");
                with.FormValue("GroupName[2]", groupName + "_2");

                with.FormValue("GroupSource[0]", groupSource + "_0");
                with.FormValue("GroupSource[1]", groupSource + "_1");
                with.FormValue("GroupSource[2]", groupSource + "_2");

                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.NoContent, postResponse.StatusCode);

            // Replace groups. _0 should be removed and _3 should be added.
            postResponse = Browser.Post("/groups/UpdateGroups", with =>
            {
                with.HttpRequest();
                with.FormValue("Id[0]", groupName + "_1");
                with.FormValue("Id[1]", groupName + "_2");
                with.FormValue("Id[2]", groupName + "_3");
                with.FormValue("GroupName[0]", groupName + "_1");
                with.FormValue("GroupName[1]", groupName + "_2");
                with.FormValue("GroupName[2]", groupName + "_3");
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.NoContent, postResponse.StatusCode);

            var getResponse0 = Browser.Get($"/groups/{groupName}_0", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            var getResponse1 = Browser.Get($"/groups/{groupName}_1", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            var getResponse2 = Browser.Get($"/groups/{groupName}_2", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            var getResponse3 = Browser.Get($"/groups/{groupName}_3", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.NotFound, getResponse0.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse1.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse2.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse3.StatusCode);

            Assert.True(getResponse1.Body.AsString().Contains(groupName + "_1"));
            Assert.True(getResponse2.Body.AsString().Contains(groupName + "_2"));
            Assert.True(getResponse3.Body.AsString().Contains(groupName + "_3"));
        }
    }
}