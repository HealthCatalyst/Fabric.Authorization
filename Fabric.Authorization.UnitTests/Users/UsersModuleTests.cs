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
        public UsersModuleTests()
        {
            SetupTestData();
            _mockGroupStore = new Mock<IGroupStore>()
                .SetupGetGroups(_existingGroups)
                .SetupGroupExists(_existingGroups);

            _mockUserStore = new Mock<IUserStore>()
                .SetupGetUser(_existingUsers);
        }

        private List<Group> _existingGroups;
        private readonly Mock<IGroupStore> _mockGroupStore;

        private List<User> _existingUsers;
        private readonly Mock<IUserStore> _mockUserStore;

        [Theory]
        [MemberData(nameof(GetPermissionsRequestData))]
        public void GetUserPermissions_Succeeds(
            string group, 
            string grain, 
            string securableItem,
            int expectedCountPermissions)
        {
            var existingClient = ExistingClients.First();
            var existingUser = _existingUsers.First();

            var usersModule = CreateBrowser(
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.ClientId, existingClient.Id),
                new Claim(JwtClaimTypes.Role, group),
                new Claim(Claims.Sub, existingUser.SubjectId),
                new Claim(Claims.IdentityProvider, existingUser.IdentityProvider)
            );
            var result = usersModule.Get("/user/permissions", with =>
                {
                    with.Query("grain", grain);
                    with.Query("securableItem", securableItem);
                })
                .Result;
            AssertOk(result, expectedCountPermissions);
        }

        [Theory]
        [MemberData(nameof(GetPermissionsForbiddenData))]
        public void GetUserPermissions_Forbidden(string securableItem, string scope)
        {
            var existingClient = ExistingClients.First();
            var usersModule = CreateBrowser(new Claim(Claims.Scope, scope),
                new Claim(Claims.ClientId, existingClient.Id),
                new Claim(JwtClaimTypes.Role, @"Fabric\Health Catalyst Admin"));

            var result = usersModule.Get("/user/permissions", with =>
                {
                    with.Query("grain", "app");
                    with.Query("securableItem", securableItem);
                })
                .Result;
            Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
        }

        [Fact]
        public void GetUserPermissions_NoParameters_DefaultsToTopLevel()
        {
            var existingClient = ExistingClients.First();
            var existingUser = _existingUsers.First();

            var usersModule = CreateBrowser(
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.ClientId, existingClient.Id),
                new Claim(JwtClaimTypes.Role, @"Fabric\Health Catalyst Admin"),
                new Claim(Claims.Sub, existingUser.SubjectId),
                new Claim(Claims.IdentityProvider, existingUser.IdentityProvider )
            );
            var result = usersModule.Get("/user/permissions").Result;
            AssertOk(result, 3);
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
            new object[] {@"Fabric\Health Catalyst Admin", "app", "patientsafety", 3},
            new object[] {@"Fabric\Health Catalyst Contributor", "app", "patientsafety", 2},
            new object[] {@"Fabric\Health Catalyst NonExistent", "app", "patientsafety", 1}
        };

        public static IEnumerable<object[]> GetPermissionsForbiddenData => new[]
        {
            new object[] {"sourcemartdesigner", Scopes.ReadScope},
            new object[] {"patientsafety", "badscope"}
        };

        protected override ConfigurableBootstrapper.ConfigurableBootstrapperConfigurator ConfigureBootstrapper(
            ConfigurableBootstrapper configurableBootstrapper,
            params Claim[] claims)
        {
            return base.ConfigureBootstrapper(configurableBootstrapper, claims)
                .Dependency<GroupService>(typeof(GroupService))
                .Dependency<ClientService>(typeof(ClientService))
                .Dependency<RoleService>(typeof(RoleService))
                .Dependency<PermissionService>(typeof(PermissionService))
                .Dependency<UserService>(typeof(UserService))
                .Dependency(_mockGroupStore.Object)
                .Dependency(_mockUserStore.Object)
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

            var customGroup = new Group
            {
                Id = "Custom Group",
                Name = "Custom Group"
            };

            _existingGroups = new List<Group>
            {
                adminGroup,
                contributorGroup,
                customGroup
            };

            _existingUsers = new List<User>
            {
                new User("user123", "Windows")
                {
                    Groups = new List<string>
                    {
                        customGroup.Name
                    }
                }
            };

            var contributorRole = new Role
            {
                Id = Guid.NewGuid(),
                Grain = "app",
                SecurableItem = "patientsafety",
                Name = "contributor"
            };

            var customGroupRole = new Role
            {
                Id = Guid.NewGuid(),
                Grain = "app",
                SecurableItem = "patientsafety",
                Name = "custom"
            };

            ExistingRoles.Add(contributorRole);
            ExistingRoles.Add(customGroupRole);

            var adminRole = ExistingRoles.First(r => r.Grain == "app"
                                                     && r.SecurableItem == "patientsafety"
                                                     && r.Name == "admin");

            adminGroup.Roles.Add(adminRole);
            adminGroup.Roles.Add(contributorRole);
            contributorGroup.Roles.Add(contributorRole);
            customGroup.Roles.Add(customGroupRole);

            adminRole.Groups.Add(adminGroup.Identifier);
            contributorRole.Groups.Add(adminGroup.Identifier);
            contributorRole.Groups.Add(contributorGroup.Identifier);
            customGroupRole.Groups.Add(customGroup.Identifier);

            var manageUsersPermission =
                ExistingPermissions.First(p => p.Grain == adminRole.Grain
                                               && p.SecurableItem == adminRole.SecurableItem
                                               && p.Name == "manageusers");

            var updatePatientPermission =
                ExistingPermissions.First(p => p.Grain == adminRole.Grain
                                               && p.SecurableItem == adminRole.SecurableItem
                                               && p.Name == "updatepatient");

            ExistingPermissions.Add(new Permission
            {
                Id = Guid.NewGuid(),
                Grain = "app",
                SecurableItem = "patientsafety",
                Name = "custom"
            });

            var customPermission =
                ExistingPermissions.First(p => p.Grain == "app"
                                               && p.SecurableItem == "patientsafety"
                                               && p.Name == "custom");

            adminRole.Permissions.Add(manageUsersPermission);
            contributorRole.Permissions.Add(updatePatientPermission);
            customGroupRole.Permissions.Add(customPermission);
        }
    }
}