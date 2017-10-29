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
using IdentityModel;
using Nancy;
using Nancy.Testing;
using Newtonsoft.Json;
using Xunit;

namespace Fabric.Authorization.IntegrationTests.Modules
{
    [Collection("InMemoryTests")]
    public class UserTests : IntegrationTestsFixture
    {
        public UserTests(bool useInMemoryDB = true)
        {
            var roleStore = useInMemoryDB
                ? new InMemoryRoleStore()
                : (IRoleStore) new CouchDbRoleStore(DbService(), Logger, EventContextResolverService);

            var userStore = useInMemoryDB
                ? new InMemoryUserStore(_identifierFormatter)
                : (IUserStore) new CouchDbUserStore(DbService(), Logger, EventContextResolverService,
                    _identifierFormatter);

            var groupStore = useInMemoryDB
                ? new InMemoryGroupStore(_identifierFormatter)
                : (IGroupStore) new CouchDbGroupStore(DbService(), Logger, EventContextResolverService,
                    _identifierFormatter);

            var clientStore = useInMemoryDB
                ? new InMemoryClientStore()
                : (IClientStore) new CouchDbClientStore(DbService(), Logger, EventContextResolverService);

            var permissionStore = useInMemoryDB
                ? new InMemoryPermissionStore(_identifierFormatter)
                : (IPermissionStore) new CouchDbPermissionStore(DbService(), Logger, EventContextResolverService,
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

        private static readonly string Group1 = Guid.Parse("A9CA0300-1006-40B1-ABF1-E0C3B396F95F").ToString();
        private static readonly string Source1 = "Source1";

        private static readonly string Group2 = Guid.Parse("ad2cea96-c020-4014-9cf6-029147454adc").ToString();
        private static readonly string Source2 = "Source2";

        private static readonly string IdentityProvider = "idP1";
        private readonly IIdentifierFormatter _identifierFormatter = new IdpIdentifierFormatter();

        [Fact]
        [DisplayTestMethodName]
        public void GetUserPermissions_NonAuthenticatedUserWithPermissions_Success()
        {
            const string groupName = "Admin";
            const string roleName = "Administrator";
            var permissionNames = new[] {"viewpatients", "editpatients", "adminpatients", "deletepatients"};
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
                    new List<PermissionApiModel> {permissionApiModels[0], permissionApiModels[1]}));

                with.Header("Accept", "application/json");
                with.Header("Content-Type", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // create 2 granular (user-based) permissions
            response = Browser.Post($"/user/{identityProvider}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();

                with.Body(JsonConvert.SerializeObject(
                    new List<PermissionApiModel> {permissionApiModels[2], permissionApiModels[3]}));

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
            Assert.NotEqual(DateTime.MinValue, permission3.CreatedDateTimeUtc);

            var permission4 = permissions.FirstOrDefault(p => p.Name == permissionNames[3]);
            Assert.NotNull(permission4);
            Assert.Equal(PermissionAction.Deny, permission4.PermissionAction);
            Assert.Equal(0, permission4.Roles.Count());
            Assert.NotEqual(DateTime.MinValue, permission4.CreatedDateTimeUtc);
        }

        [Fact]
        [DisplayTestMethodName]
        public void Test_AddGranularPermission_AllowDenyPermissionInSameRequest()
        {
            var allowReadPatientPermission = new PermissionApiModel
            {
                Grain = "app",
                SecurableItem = "userprincipal",
                Name = "readpatient",
                PermissionAction = PermissionAction.Allow
            };

            var denyReadPatientPermission = new PermissionApiModel
            {
                Grain = "app",
                SecurableItem = "userprincipal",
                Name = "readpatient",
                PermissionAction = PermissionAction.Deny
            };

            var response = Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.JsonBody(allowReadPatientPermission);
            }).Result;

            allowReadPatientPermission.Id = response.Body.DeserializeJson<PermissionApiModel>().Id;
            denyReadPatientPermission.Id = allowReadPatientPermission.Id;

            string subjectId = "userprincipal";

            var postResponse = Browser.Post($"/user/{IdentityProvider}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                var perms = new List<PermissionApiModel> { allowReadPatientPermission, denyReadPatientPermission };
                with.JsonBody(perms);
            }).Result;

            Assert.Equal(HttpStatusCode.Conflict, postResponse.StatusCode);
            Assert.Contains("The following permissions cannot be specified as both 'allow' and 'deny': app/userprincipal.readpatient", postResponse.Body.AsString());
        }

        [Fact]
        [DisplayTestMethodName]
        public void Test_AddGranularPermission_Duplicate()
        {
            var modifyPatientPermission = new PermissionApiModel
            {
                Grain = "app",
                SecurableItem = "userprincipal",
                Name = "modifypatient",
                PermissionAction = PermissionAction.Allow
            };

            var response = Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.JsonBody(modifyPatientPermission);
            }).Result;

            modifyPatientPermission.Id = response.Body.DeserializeJson<PermissionApiModel>().Id;

            var subjectId = "userprincipal";

            Browser.Post($"/user/{IdentityProvider}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                var perms = new List<PermissionApiModel> {modifyPatientPermission};
                with.JsonBody(perms);
            }).Wait();

            var postResponse = Browser.Post($"/user/{IdentityProvider}/{subjectId.ToUpper()}/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                var perms = new List<PermissionApiModel> {modifyPatientPermission};
                with.JsonBody(perms);
            }).Result;

            Assert.Equal(HttpStatusCode.Conflict, postResponse.StatusCode);
            Assert.Contains(
                "The following permissions already exist as 'allow' permissions: app/userprincipal.modifypatient",
                postResponse.Body.AsString());
        }

        [Fact]
        [DisplayTestMethodName]
        public void Test_AddGranularPermission_ExistWithOtherAction_Duplicate()
        {
            var modifyPatientPermission = new PermissionApiModel
            {
                Grain = "app",
                SecurableItem = "userprincipal",
                Name = "modifypatient",
                PermissionAction = PermissionAction.Allow
            };

            var deletePatientPermission = new PermissionApiModel
            {
                Grain = "app",
                SecurableItem = "userprincipal",
                Name = "deletepatient",
                PermissionAction = PermissionAction.Allow
            };

            var readPatientPermission = new PermissionApiModel
            {
                Grain = "app",
                SecurableItem = "userprincipal",
                Name = "readpatient",
                PermissionAction = PermissionAction.Allow
            };

            var response = Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.JsonBody(modifyPatientPermission);
            }).Result;

            modifyPatientPermission.Id = response.Body.DeserializeJson<PermissionApiModel>().Id;

            response = Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.JsonBody(deletePatientPermission);
            }).Result;

            deletePatientPermission.Id = response.Body.DeserializeJson<PermissionApiModel>().Id;

            response = Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.JsonBody(readPatientPermission);
            }).Result;

            readPatientPermission.Id = response.Body.DeserializeJson<PermissionApiModel>().Id;

            var subjectId = "userprincipal";

            Browser.Post($"/user/{IdentityProvider}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                var perms = new List<PermissionApiModel>
                {
                    modifyPatientPermission,
                    deletePatientPermission,
                    readPatientPermission
                };
                with.JsonBody(perms);
            }).Wait();

            modifyPatientPermission.PermissionAction = PermissionAction.Deny;

            var postResponse = Browser.Post($"/user/{IdentityProvider}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                var perms = new List<PermissionApiModel>
                {
                    modifyPatientPermission,
                    deletePatientPermission,
                    readPatientPermission
                };
                with.JsonBody(perms);
            }).Result;

            Assert.Equal(HttpStatusCode.Conflict, postResponse.StatusCode);
            Assert.Contains(
                "The following permissions exist as 'allow' and cannot be added as 'deny': app/userprincipal.modifypatient",
                postResponse.Body.AsString());
            Assert.Contains(
                "The following permissions already exist as 'allow' permissions: app/userprincipal.deletepatient, app/userprincipal.readpatient",
                postResponse.Body.AsString());
        }

        [Fact]
        [DisplayTestMethodName]
        public void Test_AddGranularPermissions_NoPermissionsInBody()
        {
            var subjectId = "userprincipal";

            var postRequest = Browser.Post($"/user/{IdentityProvider}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                var perms = new List<PermissionApiModel>();
                with.JsonBody(perms);
            }).Result;

            Assert.Equal(HttpStatusCode.BadRequest, postRequest.StatusCode);
        }

        [Fact]
        [DisplayTestMethodName]
        public void Test_AddGranularPermssion_ExistsWithOtherAction()
        {
            var modifyPatientPermission = new PermissionApiModel
            {
                Grain = "app",
                SecurableItem = "userprincipal",
                Name = "modifypatient",
                PermissionAction = PermissionAction.Allow
            };

            var response = Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.JsonBody(modifyPatientPermission);
            }).Result;

            modifyPatientPermission.Id = response.Body.DeserializeJson<PermissionApiModel>().Id;

            var subjectId = "userprincipal";

            Browser.Post($"/user/{IdentityProvider}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                var perms = new List<PermissionApiModel> {modifyPatientPermission};
                with.JsonBody(perms);
            }).Wait();

            modifyPatientPermission.PermissionAction = PermissionAction.Deny;

            var postResponse = Browser.Post($"/user/{IdentityProvider}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                var perms = new List<PermissionApiModel> {modifyPatientPermission};
                with.JsonBody(perms);
            }).Result;

            Assert.Equal(HttpStatusCode.Conflict, postResponse.StatusCode);
            Assert.Contains(
                "The following permissions exist as 'allow' and cannot be added as 'deny': app/userprincipal.modifypatient",
                postResponse.Body.AsString());
        }

        [Fact]
        [DisplayTestMethodName]
        public void Test_Delete_Success()
        {
            // Adding permission
            var modifyPatientPermission = new PermissionApiModel
            {
                Grain = "app",
                SecurableItem = "userprincipal",
                Name = "modifypatient",
                PermissionAction = PermissionAction.Allow
            };

            var response = Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.JsonBody(modifyPatientPermission);
            }).Result;

            modifyPatientPermission.Id = response.Body.DeserializeJson<PermissionApiModel>().Id;

            var subjectId = "userprincipal";
            Browser.Post($"/user/{IdentityProvider}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                var perms = new List<PermissionApiModel> {modifyPatientPermission};
                with.JsonBody(perms);
            }).Wait();

            // Get the permissions
            var get = Browser.Get("/user/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            var permissions = get.Body.DeserializeJson<UserPermissionsApiModel>();
            Assert.Contains("app/userprincipal.modifypatient", permissions.Permissions);

            //delete the permission
            Browser.Delete($"/user/{IdentityProvider}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                var perms = new List<PermissionApiModel> {modifyPatientPermission};
                with.JsonBody(perms);
            }).Wait();

            // Get the permissions
            get = Browser.Get("/user/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            permissions = get.Body.DeserializeJson<UserPermissionsApiModel>();
            Assert.DoesNotContain("app/userprincipal.modifypatient", permissions.Permissions);
        }

        [Fact]
        [DisplayTestMethodName]
        public void Test_Delete_UserHasNoGranularPermissions()
        {
            // Get the permissions
            var get = Browser.Get("/user/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            var permissions = get.Body.DeserializeJson<UserPermissionsApiModel>();
            Assert.Equal(0, permissions.Permissions.Count());

            var modifyPatientPermission = new PermissionApiModel
            {
                Grain = "app",
                SecurableItem = "userprincipal",
                Name = "modifypatient",
                PermissionAction = PermissionAction.Allow
            };

            var subjectId = "userprincipal";

            //attempt to delete a permission the user does not have 
            var deleteRequest = Browser.Delete($"/user/{IdentityProvider}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                var perms = new List<PermissionApiModel> {modifyPatientPermission};
                with.JsonBody(perms);
            }).Result;

            Assert.Equal(HttpStatusCode.BadRequest, deleteRequest.StatusCode);
            Assert.Contains(
                "The following permissions do not exist as 'allow' permissions: app/userprincipal.modifypatient",
                deleteRequest.Body.AsString());
            Assert.DoesNotContain("The following permissions exist as 'deny' for user but 'allow' was specified",
                deleteRequest.Body.AsString());
        }

        [Fact]
        [DisplayTestMethodName]
        public void Test_Delete_WrongPermissionAction()
        {
            var modifyPatientPermission = new PermissionApiModel
            {
                Grain = "app",
                SecurableItem = "userprincipal",
                Name = "modifypatient",
                PermissionAction = PermissionAction.Allow
            };

            var response = Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.JsonBody(modifyPatientPermission);
            }).Result;

            modifyPatientPermission.Id = response.Body.DeserializeJson<PermissionApiModel>().Id;

            var subjectId = "userprincipal";

            Browser.Post($"/user/{IdentityProvider}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                var perms = new List<PermissionApiModel> {modifyPatientPermission};
                with.JsonBody(perms);
            }).Wait();

            // Get the permissions
            var get = Browser.Get("/user/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            var permissions = get.Body.DeserializeJson<UserPermissionsApiModel>();
            Assert.Contains("app/userprincipal.modifypatient", permissions.Permissions);

            //attempt to delete modifyPatientPermission with permission action Deny
            modifyPatientPermission.PermissionAction = PermissionAction.Deny;

            var deleteRequest = Browser.Delete($"/user/{IdentityProvider}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                var perms = new List<PermissionApiModel> {modifyPatientPermission};
                with.JsonBody(perms);
            }).Result;

            Assert.Equal(HttpStatusCode.BadRequest, deleteRequest.StatusCode);
            Assert.Contains(
                "The following permissions exist as 'allow' for user but 'deny' was specified: app/userprincipal.modifypatient",
                deleteRequest.Body.AsString());
            Assert.DoesNotContain("The permissions do not exist as 'deny' permissions", deleteRequest.Body.AsString());
        }

        [Fact]
        [DisplayTestMethodName]
        public void Test_Delete_WrongPermissionAction_InvalidPermission()
        {
            var modifyPatientPermission = new PermissionApiModel
            {
                Grain = "app",
                SecurableItem = "userprincipal",
                Name = "modifypatient",
                PermissionAction = PermissionAction.Allow
            };

            var response = Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.JsonBody(modifyPatientPermission);
            }).Result;

            modifyPatientPermission.Id = response.Body.DeserializeJson<PermissionApiModel>().Id;

            var subjectId = "userprincipal";

            Browser.Post($"/user/{IdentityProvider}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                var perms = new List<PermissionApiModel> {modifyPatientPermission};
                with.JsonBody(perms);
            }).Wait();

            // Get the permissions
            var get = Browser.Get("/user/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            var permissions = get.Body.DeserializeJson<UserPermissionsApiModel>();
            Assert.Contains("app/userprincipal.modifypatient", permissions.Permissions);

            //attempt to delete modifyPatientPermission with permission action Deny and include an invalid permission
            modifyPatientPermission.PermissionAction = PermissionAction.Deny;

            var deletePatientPermission = new PermissionApiModel
            {
                Grain = "app",
                SecurableItem = "userprincipal",
                Name = "deletepatient",
                PermissionAction = PermissionAction.Allow
            };

            var deleteRequest = Browser.Delete($"/user/{IdentityProvider}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                var perms = new List<PermissionApiModel> {modifyPatientPermission, deletePatientPermission};
                with.JsonBody(perms);
            }).Result;

            Assert.Equal(HttpStatusCode.BadRequest, deleteRequest.StatusCode);
            Assert.Contains(
                "The following permissions exist as 'allow' for user but 'deny' was specified: app/userprincipal.modifypatient",
                deleteRequest.Body.AsString());
            Assert.DoesNotContain("The permissions do not exist as 'deny' permissions", deleteRequest.Body.AsString());
            Assert.Contains(
                "The following permissions do not exist as 'allow' permissions: app/userprincipal.deletepatient",
                deleteRequest.Body.AsString());
            Assert.DoesNotContain("The following permissions exist as 'deny' for user but 'allow' was specified",
                deleteRequest.Body.AsString());
        }

        [Fact]
        [DisplayTestMethodName]
        public void Test_DeleteGranularPermissions_NoPermissionsInBody()
        {
            var subjectId = "userprincipal";

            var deleteRequest = Browser.Delete($"/user/{IdentityProvider}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                var perms = new List<PermissionApiModel>();
                with.JsonBody(perms);
            }).Result;

            Assert.Equal(HttpStatusCode.BadRequest, deleteRequest.StatusCode);
        }

        [Fact]
        [DisplayTestMethodName]
        public void TestGetPermissions_Success()
        {
            // Adding permissions
            var post = Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "userprincipal");
                with.FormValue("Name", "viewpatient");
            }).Result;

            var viewPatientPermission = post.Body.DeserializeJson<PermissionApiModel>();

            post = Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "userprincipal");
                with.FormValue("Name", "editpatient");
            }).Result;

            var editPatientPermission = post.Body.DeserializeJson<PermissionApiModel>();

            var role = new RoleApiModel
            {
                Grain = "app",
                SecurableItem = "userprincipal",
                Name = "viewer",
                Permissions = new List<PermissionApiModel> {viewPatientPermission}
            };

            post = Browser.Post("/roles", with => // -3
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.JsonBody(role);
            }).Result;

            var viewerRole = post.Body.DeserializeJson<RoleApiModel>();

            post = Browser.Post("/roles", with => // -2
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                role.Name = "editor";
                role.Permissions = new List<PermissionApiModel> {editPatientPermission};
                with.JsonBody(role);
            }).Result;

            var editorRole = post.Body.DeserializeJson<RoleApiModel>();

            Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("Id", Group1);
                with.FormValue("GroupName", Group1);
                with.FormValue("GroupSource", Source1);
                with.Header("Accept", "application/json");
            }).Wait();

            Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("Id", Group2);
                with.FormValue("GroupName", Group2);
                with.FormValue("GroupSource", Source2);
                with.Header("Accept", "application/json");
            }).Wait();

            Browser.Post($"/groups/{Group1}/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Id", viewerRole.Identifier);
            }).Wait();

            Browser.Post($"/groups/{Group2}/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Id", editorRole.Identifier);
            }).Wait();

            // Get the permissions
            var get = Browser.Get("/user/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            var permissions = get.Body.DeserializeJson<UserPermissionsApiModel>();
            Assert.Contains("app/userprincipal.editpatient", permissions.Permissions);
            Assert.Contains("app/userprincipal.viewpatient", permissions.Permissions);
            Assert.Equal(2, permissions.Permissions.Count());
        }

        [Fact]
        [DisplayTestMethodName]
        public void TestInheritance_Success()
        {
            var group = Group1;

            // Adding permissions
            var post = Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "userprincipal");
                with.FormValue("Name", "greatgrandfatherpermissions");
            }).Result;

            var ggfperm = post.Body.DeserializeJson<PermissionApiModel>();

            post = Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "userprincipal");
                with.FormValue("Name", "grandfatherpermissions");
            }).Result;

            var gfperm = post.Body.DeserializeJson<PermissionApiModel>();

            post = Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "userprincipal");
                with.FormValue("Name", "fatherpermissions");
            }).Result;

            var fperm = post.Body.DeserializeJson<PermissionApiModel>();

            post = Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "userprincipal");
                with.FormValue("Name", "himselfpermissions");
            }).Result;

            var hsperm = post.Body.DeserializeJson<PermissionApiModel>();

            post = Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "userprincipal");
                with.FormValue("Name", "sonpermissions");
            }).Result;

            var sonperm = post.Body.DeserializeJson<PermissionApiModel>();

            // Adding Roles
            var role = new RoleApiModel
            {
                Grain = "app",
                SecurableItem = "userprincipal",
                Name = "greatgrandfather",
                Permissions = new List<PermissionApiModel> {ggfperm}
            };

            post = Browser.Post("/roles", with => // -3
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.JsonBody(role);
            }).Result;

            var ggf = post.Body.DeserializeJson<RoleApiModel>();

            post = Browser.Post("/roles", with => // -2
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                role.Name = "grandfather";
                role.ParentRole = ggf.Id;
                role.Permissions = new List<PermissionApiModel> {gfperm};
                with.JsonBody(role);
            }).Result;

            var gf = post.Body.DeserializeJson<RoleApiModel>();

            post = Browser.Post("/roles", with => // -1
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                role.Name = "father";
                role.ParentRole = gf.Id;
                role.Permissions = new List<PermissionApiModel> {fperm};
                with.JsonBody(role);
            }).Result;

            var f = post.Body.DeserializeJson<RoleApiModel>();

            post = Browser.Post("/roles", with => // 0
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                role.Name = "himself";
                role.ParentRole = f.Id;
                role.Permissions = new List<PermissionApiModel> {hsperm};
                with.JsonBody(role);
            }).Result;

            var hs = post.Body.DeserializeJson<RoleApiModel>();

            post = Browser.Post("/roles", with => // 1
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                role.Name = "son";
                role.ParentRole = hs.Id;
                role.Permissions = new List<PermissionApiModel> {sonperm};
                with.JsonBody(role);
            }).Result;

            post.Body.DeserializeJson<RoleApiModel>();

            // Adding groups
            Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("Id", group);
                with.FormValue("GroupName", group);
                with.FormValue("GroupSource", Source1);
                with.Header("Accept", "application/json");
            }).Wait();

            Browser.Post($"/groups/{group}/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Id", hs.Identifier);
                with.Header("Accept", "application/json");
            }).Wait();

            // Get the permissions
            var get = Browser.Get("/user/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            Assert.True(get.Body.AsString().Contains("greatgrandfatherpermissions"));
            Assert.True(get.Body.AsString().Contains("grandfatherpermissions"));
            Assert.True(get.Body.AsString().Contains("fatherpermissions"));
            Assert.True(get.Body.AsString().Contains("himselfpermissions"));
            Assert.False(get.Body.AsString().Contains("sonpermissions"));
        }

        [Fact]
        [DisplayTestMethodName]
        public void TestRoleBlacklist_Success()
        {
            // Adding permissions
            var post = Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "userprincipal");
                with.FormValue("Name", "viewpatient");
            }).Result;

            var viewPatientPermission = post.Body.DeserializeJson<PermissionApiModel>();

            post = Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "userprincipal");
                with.FormValue("Name", "editpatient");
            }).Result;

            var editPatientPermission = post.Body.DeserializeJson<PermissionApiModel>();

            // Adding roles
            var role = new RoleApiModel
            {
                Grain = "app",
                SecurableItem = "userprincipal",
                Name = "viewer",
                Permissions = new List<PermissionApiModel> {viewPatientPermission}
            };

            post = Browser.Post("/roles", with => // -3
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.JsonBody(role);
            }).Result;

            var viewerRole = post.Body.DeserializeJson<RoleApiModel>();

            post = Browser.Post("/roles", with => // -2
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                role.Name = "editor";
                role.Permissions = new List<PermissionApiModel> {editPatientPermission};

                // Role denies viewPatient permission
                role.DeniedPermissions = new List<PermissionApiModel> {viewPatientPermission};
                with.JsonBody(role);
            }).Result;

            var editorRole = post.Body.DeserializeJson<RoleApiModel>();

            // Adding groups
            Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("Id", Group1);
                with.FormValue("GroupName", Group1);
                with.FormValue("GroupSource", Source1);
                with.Header("Accept", "application/json");
            }).Wait();

            Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("Id", Group2);
                with.FormValue("GroupName", Group2);
                with.FormValue("GroupSource", Source2);
                with.Header("Accept", "application/json");
            }).Wait();

            Browser.Post($"/groups/{Group1}/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Id", viewerRole.Identifier);
            }).Wait();

            Browser.Post($"/groups/{Group2}/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Id", editorRole.Identifier);
            }).Wait();

            // Get the permissions
            var get = Browser.Get("/user/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            var permissions = get.Body.DeserializeJson<UserPermissionsApiModel>();
            Assert.Contains("app/userprincipal.editpatient", permissions.Permissions);
            Assert.DoesNotContain("app/userprincipal.viewpatient", permissions.Permissions); // Denied by role
            Assert.Equal(1, permissions.Permissions.Count());
        }

        [Fact]
        [DisplayTestMethodName]
        public void TestUserBlacklist_Success()
        {
            // Adding permissions
            var post = Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "userprincipal");
                with.FormValue("Name", "viewpatient");
            }).Result;

            var viewPatientPermission = post.Body.DeserializeJson<PermissionApiModel>();

            post = Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "userprincipal");
                with.FormValue("Name", "editpatient");
            }).Result;

            var editPatientPermission = post.Body.DeserializeJson<PermissionApiModel>();

            // Adding roles
            var role = new RoleApiModel
            {
                Grain = "app",
                SecurableItem = "userprincipal",
                Name = "viewer",
                Permissions = new List<PermissionApiModel> {viewPatientPermission}
            };

            post = Browser.Post("/roles", with => // -3
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.JsonBody(role);
            }).Result;

            var viewerRole = post.Body.DeserializeJson<RoleApiModel>();

            post = Browser.Post("/roles", with => // -2
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                role.Name = "editor";
                role.Permissions = new List<PermissionApiModel> {editPatientPermission};
                with.JsonBody(role);
            }).Result;

            var editorRole = post.Body.DeserializeJson<RoleApiModel>();

            // Adding groups
            Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("Id", Group1);
                with.FormValue("GroupName", Group1);
                with.FormValue("GroupSource", Source1);
                with.Header("Accept", "application/json");
            }).Wait();

            Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("Id", Group2);
                with.FormValue("GroupName", Group2);
                with.FormValue("GroupSource", Source2);
                with.Header("Accept", "application/json");
            }).Wait();

            Browser.Post($"/groups/{Group1}/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Id", viewerRole.Identifier);
            }).Wait();

            Browser.Post($"/groups/{Group2}/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Id", editorRole.Identifier);
            }).Wait();

            // Adding blacklist (user cannot edit patient, even though role allows)
            var subjectId = "userprincipal";

            editPatientPermission.PermissionAction = PermissionAction.Deny;

            Browser.Post($"/user/{IdentityProvider}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                var perms = new List<PermissionApiModel> {editPatientPermission};
                with.JsonBody(perms);
            }).Wait();

            // Get the permissions
            var get = Browser.Get("/user/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            var permissions = get.Body.DeserializeJson<UserPermissionsApiModel>();
            Assert.DoesNotContain("app/userprincipal.editpatient", permissions.Permissions);
            Assert.Contains("app/userprincipal.viewpatient", permissions.Permissions);
            Assert.Equal(1, permissions.Permissions.Count());
        }

        [Fact]
        [DisplayTestMethodName]
        public void TestUserWhitelist_Success()
        {
            // Adding permissions
            var post = Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "userprincipal");
                with.FormValue("Name", "viewpatient");
            }).Result;

            var viewPatientPermission = post.Body.DeserializeJson<PermissionApiModel>();

            post = Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "userprincipal");
                with.FormValue("Name", "editpatient");
            }).Result;

            var editPatientPermission = post.Body.DeserializeJson<PermissionApiModel>();

            // Adding roles
            var role = new RoleApiModel
            {
                Grain = "app",
                SecurableItem = "userprincipal",
                Name = "viewer",
                Permissions = new List<PermissionApiModel> {viewPatientPermission}
            };

            post = Browser.Post("/roles", with => // -3
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.JsonBody(role);
            }).Result;

            var viewerRole = post.Body.DeserializeJson<RoleApiModel>();

            post = Browser.Post("/roles", with => // -2
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                role.Name = "editor";
                role.Permissions = new List<PermissionApiModel> {editPatientPermission};
                with.JsonBody(role);
            }).Result;

            var editorRole = post.Body.DeserializeJson<RoleApiModel>();

            // Adding groups
            Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("Id", Group1);
                with.FormValue("GroupName", Group1);
                with.FormValue("GroupSource", Source1);
                with.Header("Accept", "application/json");
            }).Wait();

            Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("Id", Group2);
                with.FormValue("GroupName", Group2);
                with.FormValue("GroupSource", Source2);
                with.Header("Accept", "application/json");
            }).Wait();

            Browser.Post($"/groups/{Group1}/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Id", viewerRole.Identifier);
            }).Wait();

            Browser.Post($"/groups/{Group2}/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Id", editorRole.Identifier);
            }).Wait();

            // Adding permission (user also can modify patient, even though role doesn't)
            var modifyPatientPermission = new PermissionApiModel
            {
                Grain = "app",
                SecurableItem = "userprincipal",
                Name = "modifypatient",
                PermissionAction = PermissionAction.Allow
            };

            var response = Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.JsonBody(modifyPatientPermission);
            }).Result;

            modifyPatientPermission.Id = response.Body.DeserializeJson<PermissionApiModel>().Id;

            var subjectId = "userprincipal";

            Browser.Post($"/user/{IdentityProvider}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                var perms = new List<PermissionApiModel> {modifyPatientPermission};
                with.JsonBody(perms);
            }).Wait();

            // Get the permissions
            var get = Browser.Get("/user/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            var permissions = get.Body.DeserializeJson<UserPermissionsApiModel>();
            Assert.Contains("app/userprincipal.editpatient", permissions.Permissions);
            Assert.Contains("app/userprincipal.viewpatient", permissions.Permissions);
            Assert.Contains("app/userprincipal.modifypatient", permissions.Permissions);
            Assert.Equal(3, permissions.Permissions.Count());
        }

        [Fact]
        [DisplayTestMethodName]
        public void Test_GetGroups_UserNotFound()
        {            
            var get = Browser.Get("/user/foo/bar/groups", with =>
                {
                    with.HttpRequest();
                    with.Header("Accept", "application/json");
                }).Result;

            Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
            Assert.Contains("User with SubjectId: bar and Identity Provider: foo was not found", get.Body.AsString());
        }
    }
}