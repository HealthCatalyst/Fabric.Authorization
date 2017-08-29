using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.API.Modules;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Domain.Stores.Services;
using Fabric.Authorization.UnitTests.Mocks;
using IdentityModel;
using Moq;
using Nancy;
using Nancy.Testing;
using Xunit;

namespace Fabric.Authorization.UnitTests.Users
{
    public class UsersModuleTests : ModuleTestsBase<UsersModule>
    {
        private List<Group> _existingGroups;

        private readonly Mock<IGroupStore> _mockGroupStore;

        public UsersModuleTests()
        {
            SetupTestData();
            _mockGroupStore = new Mock<IGroupStore>()
                .SetupGetGroups(_existingGroups)
                .SetupGroupExists(_existingGroups);
        }

        [Theory, MemberData(nameof(GetPermissionsRequestData))]
        public void GetUserPermissions_Succeeds(string group, string grain, string securableItem, int expectedCountPermissions)
        {
            var existingClient = ExistingClients.First();
            var usersModule = CreateBrowser(
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.ClientId, existingClient.Id), 
                new Claim(JwtClaimTypes.Role, group),
                new Claim(Claims.Sub, existingClient.Id)
            );
            var result = usersModule.Get("/users/permissions", with =>
                {
                    with.Query("grain", grain);
                    with.Query("securableItem", securableItem);
                })
                .Result;
            AssertOk(result, expectedCountPermissions);
        }

        [Fact]
        public void GetUserPermissions_NoParameters_DefaultsToTopLevel()
        {
            var existingClient = ExistingClients.First();
            var usersModule = CreateBrowser(
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.ClientId, existingClient.Id), 
                new Claim(JwtClaimTypes.Role, @"Fabric\Health Catalyst Admin"),
                new Claim(Claims.Sub, existingClient.Id)
                );
            var result = usersModule.Get("/users/permissions").Result;
            AssertOk(result, 2);
        }

        [Theory, MemberData(nameof(GetPermissionsForbiddenData))]
        public void GetUserPermissions_Forbidden(string securableItem, string scope)
        {
            var existingClient = ExistingClients.First();
            var usersModule = CreateBrowser(new Claim(Claims.Scope, scope),
                new Claim(Claims.ClientId, existingClient.Id), new Claim(JwtClaimTypes.Role, @"Fabric\Health Catalyst Admin"));
            var result = usersModule.Get("/users/permissions", with =>
                {
                    with.Query("grain", "app");
                    with.Query("securableItem", securableItem);
                })
                .Result;
            Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
        }

        private void AssertOk(BrowserResponse result, int expectedCountPermissions)
        {
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var permissions = result.Body.DeserializeJson<UserPermissionsApiModel>();
            Assert.NotNull(permissions);
            Assert.Equal(expectedCountPermissions, permissions.Permissions.Count());
        }

        public static IEnumerable<object[]> GetPermissionsRequestData => new[]
        {
            new object[] {@"Fabric\Health Catalyst Admin", "app", "patientsafety", 2},
            new object[] {@"Fabric\Health Catalyst Contributor", "app", "patientsafety", 1},
            new object[] {@"Fabric\Health Catalyst NonExistant", "app", "patientsafety", 0},
        };

        public static IEnumerable<object[]> GetPermissionsForbiddenData => new[]
        {
            new object [] { "sourcemartdesigner", Scopes.ReadScope },
            new object [] { "patientsafety", "badscope" },
        };

        protected override ConfigurableBootstrapper.ConfigurableBootstrapperConfigurator ConfigureBootstrapper(ConfigurableBootstrapper configurableBootstrapper,
            params Claim[] claims)
        {
            return base.ConfigureBootstrapper(configurableBootstrapper, claims)
                .Dependency<GroupService>(typeof(GroupService))
                .Dependency<ClientService>(typeof(ClientService))
                .Dependency<RoleService>(typeof(RoleService))
                .Dependency<PermissionService>(typeof(PermissionService))
                .Dependency(_mockGroupStore.Object)
                .Dependency(MockUserStore.Object)
                .Dependency(MockLogger.Object)
                .Dependency(MockClientStore.Object)
                .Dependency(MockRoleStore.Object)
                .Dependency(MockPermissionStore.Object);
        }

        private void SetupTestData()
        {
            var adminGroup = new Group
            {
                Id = @"Fabric\Health Catalyst Admin",
                Name = @"Fabric\Health Catalyst Admin"
            };

            var contributorGroup = new Group
            {
                Id = @"Fabric\Health Catalyst Contributor",
                Name = @"Fabric\Health Catalyst Contributor"
            };

            _existingGroups = new List<Group>
            {
                adminGroup,
                contributorGroup
            };

            var contributorRole = new Role
            {
                Id = Guid.NewGuid(),
                Grain = "app",
                SecurableItem = "patientsafety",
                Name = "contributor"
            };
            ExistingRoles.Add(contributorRole);

            var adminRole = ExistingRoles.First(r => r.Grain == "app" && r.SecurableItem == "patientsafety" &&
                                                     r.Name == "admin");

            adminGroup.Roles.Add(adminRole);
            adminGroup.Roles.Add(contributorRole);
            contributorGroup.Roles.Add(contributorRole);

            adminRole.Groups.Add(adminGroup.Identifier);
            contributorRole.Groups.Add(adminGroup.Identifier);
            contributorRole.Groups.Add(contributorGroup.Identifier);

            var manageUsersPermission =
                ExistingPermissions.First(p => p.Grain == adminRole.Grain &&
                                               p.SecurableItem == adminRole.SecurableItem && p.Name == "manageusers");

            var updatePatientPermission =
                ExistingPermissions.First(p => p.Grain == adminRole.Grain &&
                                               p.SecurableItem == adminRole.SecurableItem && p.Name == "updatepatient");

            adminRole.Permissions.Add(manageUsersPermission);
            contributorRole.Permissions.Add(updatePatientPermission);
        }
    }
}