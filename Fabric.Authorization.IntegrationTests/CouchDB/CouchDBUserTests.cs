using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Infrastructure.PipelineHooks;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.API.Modules;
using Fabric.Authorization.Domain.Resolvers.Permissions;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Domain.Stores.CouchDB;
using Fabric.Authorization.Domain.Stores.InMemory;
using Fabric.Authorization.Domain.Stores.Services;
using Fabric.Authorization.Domain.Validators;
using Fabric.Authorization.IntegrationTests.Modules;
using IdentityModel;
using Nancy.Testing;
using Newtonsoft.Json;
using Xunit;
using Nancy;

namespace Fabric.Authorization.IntegrationTests.CouchDB
{
    [Collection("CouchTests")]
    public class CouchDBUserTests : IntegrationTestsFixture
    {
        private static readonly string Group1 = Guid.Parse("A9CA0300-1006-40B1-ABF1-E0C3B396F95F").ToString();
        private static readonly string Source1 = "Source1";

        private static readonly string Group2 = Guid.Parse("ad2cea96-c020-4014-9cf6-029147454adc").ToString();
        private static readonly string Source2 = "Source2";

        private static readonly string IdentityProvider = "idP1";
        private readonly IIdentifierFormatter _identifierFormatter = new IdpIdentifierFormatter();

        public CouchDBUserTests()
        {
            var useInMemoryDB = false;
            var roleStore = useInMemoryDB
                ? new InMemoryRoleStore()
                : (IRoleStore)new CouchDbRoleStore(DbService(), Logger, EventContextResolverService);

            var userStore = useInMemoryDB
                ? new InMemoryUserStore(_identifierFormatter)
                : (IUserStore)new CouchDbUserStore(DbService(), Logger, EventContextResolverService,
                    _identifierFormatter);

            var groupStore = useInMemoryDB
                ? new InMemoryGroupStore(_identifierFormatter)
                : (IGroupStore)new CouchDbGroupStore(DbService(), Logger, EventContextResolverService,
                    _identifierFormatter);

            var clientStore = useInMemoryDB
                ? new InMemoryClientStore()
                : (IClientStore)new CouchDbClientStore(DbService(), Logger, EventContextResolverService);

            var permissionStore = useInMemoryDB
                ? new InMemoryPermissionStore(_identifierFormatter)
                : (IPermissionStore)new CouchDbPermissionStore(DbService(), Logger, EventContextResolverService,
                    _identifierFormatter);

            var clientService = new ClientService(clientStore);
            var roleService = new RoleService(roleStore, permissionStore, clientService);
            var groupService = new GroupService(groupStore, roleStore, userStore, roleService);
            var userService = new UserService(userStore);
            var permissionService = new PermissionService(permissionStore, roleService);
            var permissionResolverService = new PermissionResolverService(roleService, permissionService,
                new List<IPermissionResolverService>
                {
                    new GranularPermissionResolverService(permissionService, Logger),
                    new RolePermissionResolverService(roleService)
                },
                Logger);

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

                with.Module(new UsersModule(
                    clientService,
                    permissionService,
                    userService,
                    roleService,
                    permissionResolverService,
                    new UserValidator(),
                    Logger));

                with.Module(new GroupsModule(
                    groupService,
                    new GroupValidator(groupService),
                    Logger));

                with.Module(new PermissionsModule(
                    permissionService,
                    clientService,
                    new PermissionValidator(permissionService),
                    Logger));

                with.RequestStartup((_, pipelines, context) =>
                {
                    context.CurrentUser = new ClaimsPrincipal(
                        new ClaimsIdentity(new List<Claim>
                        {
                            new Claim(Claims.Scope, Scopes.ManageClientsScope),
                            new Claim(Claims.Scope, Scopes.ReadScope),
                            new Claim(Claims.Scope, Scopes.WriteScope),
                            new Claim(Claims.ClientId, "userprincipal"),
                            new Claim(Claims.Sub, "userprincipal"),
                            new Claim(JwtClaimTypes.Role, Group1),
                            new Claim(JwtClaimTypes.Role, Group2),
                            new Claim(JwtClaimTypes.IdentityProvider, IdentityProvider)
                        }, "userprincipal"));
                    pipelines.BeforeRequest += ctx => RequestHooks.SetDefaultVersionInUrl(ctx);
                });
            }, withDefaults => withDefaults.HostName("testhost"));

            Browser.Post("/clients", with =>
            {
                with.HttpRequest();
                with.FormValue("Id", "userprincipal");
                with.FormValue("Name", "userprincipal");
                with.Header("Accept", "application/json");
            }).Wait();
        }

        [Fact]
        [DisplayTestMethodName]
        public void GetUserPermissions_NonAuthenticatedUserWithPermissions_Success()
        {
            const string groupName = "Admin";
            const string roleName = "Administrator";
            var permissionNames = new[] { "viewpatients", "editpatients", "adminpatients", "deletepatients" };
            const string subjectId = "first.last";
            const string identityProvider = "Windows";

            // add custom group
            var response = Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("GroupName", groupName);
                with.FormValue("GroupSource", "Custom");
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // add role
            response = Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "userprincipal");
                with.FormValue("Name", roleName);
            }).Result;

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var roleId = response.Body.DeserializeJson<RoleApiModel>().Id;

            // add role to group
            response = Browser.Post($"/groups/{groupName}/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Id", roleId.ToString());
            }).Result;

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // add 4 permissions
            response = Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "userprincipal");
                with.FormValue("Name", permissionNames[0]);
            }).Result;

            var permission1Id = response.Body.DeserializeJson<PermissionApiModel>().Id;

            response = Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "userprincipal");
                with.FormValue("Name", permissionNames[1]);
            }).Result;

            var permission2Id = response.Body.DeserializeJson<PermissionApiModel>().Id;

            response = Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "userprincipal");
                with.FormValue("Name", permissionNames[2]);
            }).Result;

            var permission3Id = response.Body.DeserializeJson<PermissionApiModel>().Id;

            response = Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "userprincipal");
                with.FormValue("Name", permissionNames[3]);
            }).Result;

            var permission4Id = response.Body.DeserializeJson<PermissionApiModel>().Id;

            // add user to group
            response = Browser.Post($"/groups/{groupName}/users", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("SubjectId", subjectId);
                with.FormValue("IdentityProvider", identityProvider);
            }).Result;

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var permissionApiModels = new List<PermissionApiModel>
            {
                new PermissionApiModel
                {
                    Id = permission1Id,
                    Grain = "app",
                    SecurableItem = "userprincipal",
                    Name = permissionNames[0],
                    PermissionAction = PermissionAction.Allow
                },
                new PermissionApiModel
                {
                    Id = permission2Id,
                    Grain = "app",
                    SecurableItem = "userprincipal",
                    Name = permissionNames[1],
                    PermissionAction = PermissionAction.Deny
                },
                new PermissionApiModel
                {
                    Id = permission3Id,
                    Grain = "app",
                    SecurableItem = "userprincipal",
                    Name = permissionNames[2],
                    PermissionAction = PermissionAction.Allow
                },
                new PermissionApiModel
                {
                    Id = permission4Id,
                    Grain = "app",
                    SecurableItem = "userprincipal",
                    Name = permissionNames[3],
                    PermissionAction = PermissionAction.Deny
                }
            };

            // create 2 role-based permissions
            response = Browser.Post($"/roles/{roleId}/permissions", with =>
            {
                with.HttpRequest();

                with.Body(JsonConvert.SerializeObject(
                    new List<PermissionApiModel> { permissionApiModels[0], permissionApiModels[1] }));

                with.Header("Accept", "application/json");
                with.Header("Content-Type", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // create 2 granular (user-based) permissions
            response = Browser.Post($"/user/{identityProvider}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();

                with.Body(JsonConvert.SerializeObject(
                    new List<PermissionApiModel> { permissionApiModels[2], permissionApiModels[3] }));

                with.Header("Accept", "application/json");
                with.Header("Content-Type", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // retrieve permissions for user
            response = Browser.Get($"/user/{identityProvider}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            var permissions = response.Body.DeserializeJson<List<PermissionApiModel>>();

            Assert.NotNull(permissions);
            Assert.Equal(4, permissions.Count);

            var permission1 = permissions.FirstOrDefault(p => p.Name == permissionNames[0]);
            Assert.NotNull(permission1);
            Assert.Equal(PermissionAction.Allow, permission1.PermissionAction);
            Assert.Equal(1, permission1.Roles.Count());

            /*var permission2 = permissions.FirstOrDefault(p => p.Name == permissionNames[1]);
            Assert.NotNull(permission2);
            Assert.Equal(PermissionAction.Deny, permission2.PermissionAction);
            Assert.Equal(1, permission2.Roles.Count());*/

            var permission3 = permissions.FirstOrDefault(p => p.Name == permissionNames[2]);
            Assert.NotNull(permission3);
            Assert.Equal(PermissionAction.Allow, permission3.PermissionAction);
            Assert.Equal(0, permission3.Roles.Count());
            Assert.NotEqual(DateTime.MinValue.ToUniversalTime(), permission3.CreatedDateTimeUtc);

            var permission4 = permissions.FirstOrDefault(p => p.Name == permissionNames[3]);
            Assert.NotNull(permission4);
            Assert.Equal(PermissionAction.Deny, permission4.PermissionAction);
            Assert.Equal(0, permission4.Roles.Count());
            Assert.NotEqual(DateTime.MinValue.ToUniversalTime(), permission4.CreatedDateTimeUtc);
        }
    }
}