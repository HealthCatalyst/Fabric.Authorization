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
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var roles = result.Body.DeserializeJson<List<RoleApiModel>>();
            Assert.Equal(1, roles.Count);
            Assert.Equal(existingRole.Id, roles.First().Id);
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
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var roles = result.Body.DeserializeJson<List<RoleApiModel>>();
            Assert.Equal(1, roles.Count);
            Assert.Equal(existingRole.Id, roles.First().Id);
        }

        [Fact]
        public void GetRoles_ReturnsNotAllowedForIncorrectCredentials()
        {
            var existingClient = _existingClients.First();
            var rolesModule = CreateBrowser(new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.ClientId, existingClient.Id));
            var result = rolesModule.Get($"/roles/app/notmysecurable").Result;
            Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
        }

        [Fact]
        public void GetRoles_ReturnsNotAllowedForIncorrectScope()
        {
            var existingClient = _existingClients.First();
            var rolesModule = CreateBrowser(new Claim(Claims.Scope, "badscope"),
                new Claim(Claims.ClientId, existingClient.Id));
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
            var rolesModule = CreateBrowser(new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.ClientId, existingClient.Id));
            var result = rolesModule.Delete($"/roles/{existingRole.Id}").Result;
            Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
        }

        [Fact]
        public void DeleteRole_ReturnsNotFound()
        {
            var existingClient = _existingClients.First();
            var rolesModule = CreateBrowser(new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.ClientId, existingClient.Id));
            var result = rolesModule.Delete($"/roles/{Guid.NewGuid()}").Result;
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        }

        [Fact]
        public void DeleteRole_ReturnsBadRequest()
        {
            var existingClient = _existingClients.First();
            var rolesModule = CreateBrowser(new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.ClientId, existingClient.Id));
            var result = rolesModule.Delete($"/roles/notaguid").Result;
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Fact]
        public void DeleteRole_WrongScope_ReturnsForbidden()
        {
            var existingClient = _existingClients.First();
            var existingRole = _existingRoles.First(r => r.SecurableItem == existingClient.Id);
            var rolesModule = CreateBrowser(new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.ClientId, existingClient.Id));
            var result = rolesModule.Delete($"/roles/{existingRole.Id}").Result;
            Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
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
