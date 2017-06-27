using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.API.Modules;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.UnitTests.Mocks;
using Moq;
using Nancy;
using Nancy.Testing;
using Serilog;
using Xunit;

namespace Fabric.Authorization.UnitTests.Roles
{
    public class RolesModuleTests : ModuleTestsBase<RolesModule>
    {
        private readonly List<Permission> _existingPermissions;
        private readonly List<Role> _existingRoles;
        private readonly List<Client> _existingClients;
        private readonly Mock<IPermissionStore> _mockPermissionStore;
        private readonly Mock<IRoleStore> _mockRoleStore;
        private readonly Mock<IClientStore> _mockClientStore;
        private readonly Mock<ILogger> _mockLogger;

        public RolesModuleTests()
        {
            _existingClients = new List<Client>
            {
                new Client
                {
                    Id = "patientsafety",
                    TopLevelSecurableItem = new SecurableItem
                    {
                        Id = Guid.NewGuid(),
                        Name = "patientsafety"
                    }
                },
                new Client
                {
                    Id = "sourcemartdesigner",
                    TopLevelSecurableItem = new SecurableItem
                    {
                        Id = Guid.NewGuid(),
                        Name = "sourcemartdesigner"
                    }
                }
            };

            _mockClientStore = new Mock<IClientStore>()
                .SetupGetClient(_existingClients)
                .SetupAddClient();

            _existingPermissions = new List<Permission>
            {
                new Permission
                {
                    Id = Guid.NewGuid(),
                    Grain = "app",
                    SecurableItem = "patientsafety",
                    Name = "manageusers"
                },
                new Permission
                {
                    Id = Guid.NewGuid(),
                    Grain = "app",
                    SecurableItem = "patientsafety",
                    Name = "updatepatient"
                },
                new Permission
                {
                    Id = Guid.NewGuid(),
                    Grain = "app",
                    SecurableItem = "sourcemartdesigner",
                    Name = "manageusers"
                },
                new Permission
                {
                    Id = Guid.NewGuid(),
                    Grain = "patient",
                    SecurableItem = "Patient",
                    Name ="read"
                }
            };
            
            _mockPermissionStore = new Mock<IPermissionStore>()
                .SetupGetPermissions(_existingPermissions)
                .SetupAddPermissions();

            _existingRoles = new List<Role>
            {
                new Role
                {
                    Id = Guid.NewGuid(),
                    Grain = "app",
                    SecurableItem = "patientsafety",
                    Name = "admin"
                },
                new Role
                {
                    Id = Guid.NewGuid(),
                    Grain = "app",
                    SecurableItem = "sourcemartdesigner",
                    Name = "admin"
                }
            };

            _mockRoleStore = new Mock<IRoleStore>()
                .SetupGetRoles(_existingRoles)
                .SetupAddRole();

            _mockLogger = new Mock<ILogger>();
        }

        [Fact]
        public void GetRoles_ReturnsRolesForGrainAndSecurableItem()
        {
            var existingClient = _existingClients.First();
            var existingRole = _existingRoles.First(r => r.SecurableItem == existingClient.Id);
            Assert.NotNull(existingRole);
            var rolesModule = CreateBrowser(new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.ClientId, existingClient.Id));
            var result = rolesModule.Get($"/roles/app/{existingClient.Id}").Result;
            AssertRolesOK(result, 1, existingRole.Id);
        }

        [Fact]
        public void GetRoles_ReturnsRoleForRoleName()
        {
            var existingClient = _existingClients.First();
            var existingRole = _existingRoles.First(r => r.SecurableItem == existingClient.Id);
            Assert.NotNull(existingRole);
            var rolesModule = CreateBrowser(new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.ClientId, existingClient.Id));
            var result = rolesModule.Get($"/roles/app/{existingRole.SecurableItem}/{existingRole.Name}").Result;
            AssertRolesOK(result, 1, existingRole.Id);
        }

        [Theory, MemberData(nameof(GetRolesForbiddenData))]
        public void GetRoles_ReturnsNotAllowedForIncorrectCredentials(string scope, string clientId)
        {
            var existingClient = _existingClients.First();
            var rolesModule = CreateBrowser(new Claim(Claims.Scope, scope),
                new Claim(Claims.ClientId, clientId));
            var result = rolesModule.Get($"/roles/app/{existingClient.Id}").Result;
            Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
        }
        
        [Fact]
        public void AddRole_Succeeds()
        {
            var existingClient = _existingClients.First();
            var rolesModule = CreateBrowser(new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.ClientId, existingClient.Id));
            var roleToPost = new RoleApiModel
            {
                Grain = "app",
                SecurableItem = existingClient.Id,
                Name = "test"
            };
            var result = rolesModule.Post($"/roles", with => with.JsonBody(roleToPost)).Result;
            Assert.Equal(HttpStatusCode.Created, result.StatusCode);
            var newRole = result.Body.DeserializeJson<RoleApiModel>();
            Assert.Equal(roleToPost.Name, newRole.Name);
            Assert.NotNull(newRole.Id);
        }

        [Theory, MemberData(nameof(AddRoleBadRequestData))]
        public void AddRole_ReturnsBadRequest(RoleApiModel roleToPost, int errorCount)
        {
            var existingClient = _existingClients.First();
            var rolesModule = CreateBrowser(new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.ClientId, existingClient.Id));
            var result = rolesModule.Post($"/roles", with => with.JsonBody(roleToPost)).Result;
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            var error = result.Body.DeserializeJson<Error>();
            Assert.NotNull(error);
            if (errorCount > 0)
            {
                Assert.Equal(errorCount, error.Details.Length);
            }
        }

        [Theory, MemberData(nameof(AddRoleForbiddenData))]
        public void AddRole_ReturnsForbidden(RoleApiModel roleToPost, string scope)
        {
            var existingClient = _existingClients.First();
            var rolesModule = CreateBrowser(new Claim(Claims.Scope, scope),
                new Claim(Claims.ClientId, existingClient.Id));
            var result = rolesModule.Post($"/roles", with => with.JsonBody(roleToPost)).Result;
            Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
        }

        [Fact]
        public void DeleteRole_Succeeds()
        {
            var existingClient = _existingClients.First();
            var existingRole = _existingRoles.First(r => r.SecurableItem == existingClient.Id);
            AssertDeleteRole(HttpStatusCode.NoContent, existingClient.Id, existingRole.Id.ToString(), Scopes.WriteScope);
        }

        [Fact]
        public void DeleteRole_ReturnsNotFound()
        {
            var existingClient = _existingClients.First();
            AssertDeleteRole(HttpStatusCode.NotFound, existingClient.Id, Guid.NewGuid().ToString(), Scopes.WriteScope);
        }

        [Fact]
        public void DeleteRole_ReturnsBadRequest()
        {
            var existingClient = _existingClients.First();
            AssertDeleteRole(HttpStatusCode.BadRequest, existingClient.Id, "notaguid", Scopes.WriteScope);
        }

        [Fact]
        public void DeleteRole_WrongScope_ReturnsForbidden()
        {
            var existingClient = _existingClients.First();
            var existingRole = _existingRoles.First(r => r.SecurableItem == existingClient.Id);
            AssertDeleteRole(HttpStatusCode.Forbidden, existingClient.Id, existingRole.Id.ToString(), Scopes.ReadScope);
        }

        [Theory, MemberData(nameof(DeleteRoleForbiddenData))]
        public void DeleteRole_WrongClient_ReturnsForbidden(string cliendId)
        {
            var existingClient = _existingClients.First();
            var existingRole = _existingRoles.First(r => r.SecurableItem == existingClient.Id);
            var rolesModule = CreateBrowser(new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.ClientId, cliendId));
            var result = rolesModule.Delete($"/roles/{existingRole.Id}").Result;
            Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
        }

        [Fact]
        public void AddPermissionsToRole_Succeeds()
        {
            var existingClient = _existingClients.First();
            var existingRole = _existingRoles.First(r => r.SecurableItem == existingClient.Id);
            var existingPermission =
                _existingPermissions.First(p => p.Grain == existingRole.Grain &&
                                                p.SecurableItem == existingRole.SecurableItem);
            var rolesModule = CreateBrowser(new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.ClientId, existingClient.Id));
            var result = rolesModule.Post($"/roles/{existingRole.Id}/permissions",
                    with => with.JsonBody(new List<Permission>{existingPermission}))
                .Result;
            AssertRoleOK(result, 1);
        }

        [Fact]
        public void AddPermissionsToRole_BadRequest()
        {
            var existingClient = _existingClients.First();
            var existingRole = _existingRoles.First(r => r.SecurableItem == existingClient.Id);
            var existingPermission =
                _existingPermissions.First(p => p.Grain == existingRole.Grain &&
                                                p.SecurableItem == existingRole.SecurableItem);
            var rolesModule = CreateBrowser(new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.ClientId, existingClient.Id));
            var result = rolesModule.Post($"/roles/{existingRole.Id}/permissions",
                    with => with.JsonBody(existingPermission))
                .Result;
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Fact]
        public void AddPermissionsToRole_RoleNotFound()
        {
            var existingClient = _existingClients.First();
            var role = new Role
            {
                Id = Guid.NewGuid(),
                Grain = "app",
                SecurableItem = "patientsafety",
                Name = "notfound"
            };
            var existingPermission =
                _existingPermissions.First(p => p.Grain == role.Grain &&
                                                p.SecurableItem == role.SecurableItem);
            PostPermissionAndAssert(role, existingPermission, existingClient.Id, HttpStatusCode.BadRequest);
        }

        [Fact]
        public void AddPermissionsToRole_PermissionNotFound()
        {
            var existingClient = _existingClients.First();
            var existingRole = _existingRoles.First(r => r.SecurableItem == existingClient.Id);
            var permission = new Permission
            {
                Id = Guid.NewGuid(),
                Grain = "app",
                SecurableItem = "patientsafety",
                Name = "notfound"
            };
            PostPermissionAndAssert(existingRole, permission, existingClient.Id, HttpStatusCode.BadRequest);
        }

        [Fact]
        public void AddPermissionsToRole_IncompatiblePermission_PermissionAlreadyExists()
        {
            var existingClient = _existingClients.First();
            var existingRole = _existingRoles.First(r => r.SecurableItem == existingClient.Id);
            var existingPermission =
                _existingPermissions.First(p => p.Grain == existingRole.Grain &&
                                                p.SecurableItem == existingRole.SecurableItem);
            existingRole.Permissions.Add(existingPermission);
            PostPermissionAndAssert(existingRole, existingPermission, existingClient.Id, HttpStatusCode.BadRequest);
        }

        [Fact]
        public void AddPermissionsToRole_IncompatiblePermission_WrongSecurable()
        {
            var existingClient = _existingClients.First();
            var existingRole = _existingRoles.First(r => r.SecurableItem == existingClient.Id);
            var existingPermission =
                _existingPermissions.First(p => p.Grain == existingRole.Grain &&
                                                p.SecurableItem != existingRole.SecurableItem);
            PostPermissionAndAssert(existingRole, existingPermission, existingClient.Id, HttpStatusCode.BadRequest);
        }

        [Theory, MemberData(nameof(AddPermissionToRoleForbiddenData))]
        public void AddPermissionsToRole_Forbidden(string clientId, string securableItem, string scope)
        {
            var role = _existingRoles.First(r => r.Grain == "app" && r.SecurableItem == securableItem);
            var permission =
                _existingPermissions.First(p => p.Grain == role.Grain && p.SecurableItem == role.SecurableItem);
            PostPermissionAndAssert(role, permission, clientId, HttpStatusCode.Forbidden, scope);
        }

        [Fact]
        public void DeletePermissionFromRole_Succeeds()
        {
            var existingClient = _existingClients.First();
            var existingRole = _existingRoles.First(r => r.SecurableItem == existingClient.Id);
            var existingPermission =
                _existingPermissions.First(p => p.Grain == existingRole.Grain &&
                                                p.SecurableItem == existingRole.SecurableItem);
            existingRole.Permissions.Add(existingPermission);
            var rolesModule = CreateBrowser(new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.ClientId, existingClient.Id));
            var result = rolesModule.Delete($"/roles/{existingRole.Id}/permissions",
                    with => with.JsonBody(new List<Permission> { existingPermission }))
                .Result;
            AssertRoleOK(result, 0);
        }

        [Fact]
        public void DeletePermissionFromRole_IncorrectFormat_BadRequest()
        {
            var existingClient = _existingClients.First();
            var existingRole = _existingRoles.First(r => r.SecurableItem == existingClient.Id);
            var existingPermission =
                _existingPermissions.First(p => p.Grain == existingRole.Grain &&
                                                p.SecurableItem == existingRole.SecurableItem);
            existingRole.Permissions.Add(existingPermission);
            var rolesModule = CreateBrowser(new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.ClientId, existingClient.Id));
            var result = rolesModule.Delete($"/roles/{existingRole.Id}/permissions",
                    with => with.JsonBody(existingPermission))
                .Result;
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Fact]
        public void DeletePermissionFromRole_RoleNotFound()
        {
            var existingClient = _existingClients.First();
            var notFoundRole = new Role
            {
                Id = Guid.NewGuid(),
                Grain = "app",
                SecurableItem = existingClient.Id,
                Name = "notfound"
            };
            var existingPermission =
                _existingPermissions.First(p => p.Grain == notFoundRole.Grain &&
                                                p.SecurableItem == notFoundRole.SecurableItem);
            notFoundRole.Permissions.Add(existingPermission);
            DeletePermissionAndAssert(existingClient.Id, notFoundRole.Id.ToString(), existingPermission, HttpStatusCode.BadRequest);
        }

        [Fact]
        public void DeletePermissionFromRole_PermissionNotFound()
        {
            var existingClient = _existingClients.First();
            var existingRole = _existingRoles.First(r => r.SecurableItem == existingClient.Id);
            var existingPermission =
                _existingPermissions.First(p => p.Grain == existingRole.Grain &&
                                                p.SecurableItem == existingRole.SecurableItem);
            DeletePermissionAndAssert(existingClient.Id, existingRole.Id.ToString(), existingPermission, HttpStatusCode.BadRequest);
        }

        [Fact]
        public void DeletePermissionFromRole_Forbidden()
        {
            var existingClient = _existingClients.First();
            var notFoundRole = _existingRoles.First(r => r.SecurableItem == existingClient.Id);
            var existingPermission =
                _existingPermissions.First(p => p.Grain == notFoundRole.Grain &&
                                                p.SecurableItem == notFoundRole.SecurableItem);
            DeletePermissionAndAssert("sourcemartdesigner", notFoundRole.Id.ToString(), existingPermission, HttpStatusCode.Forbidden);
        }

        [Fact]
        public void DeletePermissionFromRole_WrongScope_Forbidden()
        {
            var existingClient = _existingClients.First();
            var existingRole = _existingRoles.First(r => r.SecurableItem == existingClient.Id);
            var existingPermission =
                _existingPermissions.First(p => p.Grain == existingRole.Grain &&
                                                p.SecurableItem == existingRole.SecurableItem);
            DeletePermissionAndAssert(existingClient.Id, existingRole.Id.ToString(), existingPermission, HttpStatusCode.Forbidden, Scopes.ReadScope);
        }

        [Fact]
        public void DeletePermissionFromRole_BadRequest()
        {
            var existingClient = _existingClients.First();
            var existingRole = _existingRoles.First(r => r.SecurableItem == existingClient.Id);
            var existingPermission =
                _existingPermissions.First(p => p.Grain == existingRole.Grain &&
                                                p.SecurableItem == existingRole.SecurableItem);
            DeletePermissionAndAssert(existingClient.Id, "notaguid", existingPermission, HttpStatusCode.BadRequest, Scopes.WriteScope);
        }

        private void DeletePermissionAndAssert(string clientId, string roleId, Permission permission, HttpStatusCode expectedStatusCode, string scope = null)
        {
            var requestScope = string.IsNullOrEmpty(scope) ? Scopes.WriteScope : scope;
            var rolesModule = CreateBrowser(new Claim(Claims.Scope, requestScope),
                new Claim(Claims.ClientId, clientId));
            var result = rolesModule.Delete($"/roles/{roleId}/permissions",
                    with => with.JsonBody(new List<Permission> { permission }))
                .Result;
            Assert.Equal(expectedStatusCode, result.StatusCode);
        }
        
        private void PostPermissionAndAssert(Role role, Permission permission, string clientId, HttpStatusCode expectedStatusCode, string scope = null)
        {
            var requestScope = string.IsNullOrEmpty(scope) ? Scopes.WriteScope : scope;
            var rolesModule = CreateBrowser(new Claim(Claims.Scope, requestScope),
                new Claim(Claims.ClientId, clientId));
            var result = rolesModule.Post($"/roles/{role.Id}/permissions",
                    with => with.JsonBody(new List<Permission> { permission }))
                .Result;
            Assert.Equal(expectedStatusCode, result.StatusCode);
        }

        private void AssertRoleOK(BrowserResponse result, int expectedPermissionCount)
        {
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var updatedRole = result.Body.DeserializeJson<RoleApiModel>();
            Assert.NotNull(updatedRole);
            Assert.Equal(expectedPermissionCount, updatedRole.Permissions.Count());
        }

        private void AssertRolesOK(BrowserResponse result, int expectedRolesCount, Guid expectedId)
        {
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var roles = result.Body.DeserializeJson<List<RoleApiModel>>();
            Assert.Equal(expectedRolesCount, roles.Count);
            Assert.Equal(expectedId, roles.First().Id);
        }
        
        private void AssertDeleteRole(HttpStatusCode expectedStatusCode, string clientId, string roleId, string scope)
        {
            var rolesModule = CreateBrowser(new Claim(Claims.Scope, scope),
                new Claim(Claims.ClientId, clientId));
            var result = rolesModule.Delete($"/roles/{roleId}").Result;
            Assert.Equal(expectedStatusCode, result.StatusCode);
        }

        public static IEnumerable<object[]> AddRoleBadRequestData => new[]
        {
            new object[] {new RoleApiModel {Grain = "app", Name = "test"}, 1},
            new object[] {new RoleApiModel {Grain = "app", SecurableItem = "patientsafety"}, 1},
            new object[] {new RoleApiModel {Grain = "app"}, 2},
            new object[] {new RoleApiModel(), 3}
        };

        public static IEnumerable<object[]> AddRoleForbiddenData => new[]
        {
            new object[] {new RoleApiModel {Grain = "app", SecurableItem = "patientsafety", Name = "test"}, Scopes.ReadScope},
            new object[] {new RoleApiModel {Grain = "app", SecurableItem = "notmyapp", Name = "test"}, Scopes.WriteScope},
        };

        public static IEnumerable<object[]> DeleteRoleForbiddenData => new[]
        {
            new object[] {"sourcemartdesigner"},
            new object[] {"notaclient"},
        };

        public static IEnumerable<object[]> AddPermissionToRoleForbiddenData => new[]
        {
            new object[] { "patientsafety", "patientsafety", Scopes.ReadScope}, 
            new object[] { "patientsafety", "sourcemartdesigner", Scopes.WriteScope}, 
        };

        public static IEnumerable<object[]> GetRolesForbiddenData => new[]
        {
            new object[] {"badscope", "patientsafety"},
            new object[] {Scopes.ReadScope, "sourcemartdesigner"},
        };

        protected override ConfigurableBootstrapper.ConfigurableBootstrapperConfigurator ConfigureBootstrapper(ConfigurableBootstrapper configurableBootstrapper,
            params Claim[] claims)
        {
            return base.ConfigureBootstrapper(configurableBootstrapper, claims)
                .Dependency<IRoleService>(typeof(RoleService))
                .Dependency<IClientService>(typeof(ClientService))
                .Dependency(_mockLogger.Object)
                .Dependency(_mockClientStore.Object)
                .Dependency(_mockPermissionStore.Object)
                .Dependency(_mockRoleStore.Object);
        }
    }
}
