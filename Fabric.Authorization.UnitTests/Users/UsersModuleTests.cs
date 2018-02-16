using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.API.Modules;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Resolvers.Permissions;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.UnitTests.Mocks;
using IdentityModel;
using Moq;
using Nancy;
using Nancy.Helpers;
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
                .SetupUserStore(_existingUsers);
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

        [Theory]
        [InlineData("test", "", "You must specify an IdentityProvider for this user")]
        [InlineData("", "test", "You must specify a SubjectId for this user")]
        public void AddUser_MissingData_ReturnsBadRequest(string subjectId, string identityProvider, string message)
        {
            var existingClient = ExistingClients.First();
            var usersModule = CreateBrowser(
                new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.ClientId, existingClient.Id)
            );

            var postResponse = usersModule.Post("/user", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    SubjectId = subjectId,
                    IdentityProvider = identityProvider
                });
            }).Result;

            Assert.Equal(HttpStatusCode.BadRequest, postResponse.StatusCode);
            var result = postResponse.Body.DeserializeJson<Error>();
            Assert.Equal(message, result.Details.First().Message);
        }

        [Fact]
        public void AddUser_MissingScope_ReturnsBadForbidden()
        {
            var existingClient = ExistingClients.First();
            var usersModule = CreateBrowser(
                new Claim(Claims.ClientId, existingClient.Id)
            );

            var postResponse = usersModule.Post("/user", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    SubjectId = "test",
                    IdentityProvider = "test"
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Forbidden, postResponse.StatusCode);
        }

        [Fact]
        public void AddUser_ExistingUser_ReturnsConflict()
        {
            var existingClient = ExistingClients.First();
            var existingUser = _existingUsers.First();
            var usersModule = CreateBrowser(
                new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.ClientId, existingClient.Id)
            );

            var postResponse = usersModule.Post("/user", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    existingUser.SubjectId,
                    existingUser.IdentityProvider
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Conflict, postResponse.StatusCode);
            var result = postResponse.Body.DeserializeJson<Error>();
            Assert.Equal($"The User {existingUser.SubjectId} already exists for the Identity Provider: {existingUser.IdentityProvider}", result.Details.First().Message);
        }

        [Fact]
        public void AddUser_NewUser_ReturnsCreated()
        {
            var existingClient = ExistingClients.First();
            var usersModule = CreateBrowser(
                new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.ClientId, existingClient.Id)
            );
            CreateUser(usersModule);
        }

        [Fact]
        public void GetUser_ExistingUser_ReturnsOk()
        {
            var existingClient = ExistingClients.First();
            var usersModule = CreateBrowser(
                new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.ClientId, existingClient.Id)
            );
            var expectedUser = CreateUser(usersModule);
            var actualUserResponse = usersModule.Get($"/user/{expectedUser.IdentityProvider}/{HttpUtility.UrlEncode(expectedUser.SubjectId)}").Result;
            Assert.Equal(HttpStatusCode.OK, actualUserResponse.StatusCode);
            var actualUser = actualUserResponse.Body.DeserializeJson<UserApiModel>();
            Assert.Equal(expectedUser.IdentityProvider, actualUser.IdentityProvider);
            Assert.Equal(expectedUser.SubjectId, actualUser.SubjectId);
        }

        [Fact]
        public void GetUser_UserDoesNotExist_ReturnsNotFound()
        {
            var existingClient = ExistingClients.First();
            var usersModule = CreateBrowser(
                new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.ClientId, existingClient.Id)
            );
            var actualUserResponse = usersModule.Get($"/user/windows/nouser").Result;
            Assert.Equal(HttpStatusCode.NotFound, actualUserResponse.StatusCode);
        }

        [Fact]
        public void AddRolesToUser_UserDoesNotExist_ReturnsNotFound()
        {
            var existingClient = ExistingClients.First();
            var usersModule = CreateBrowser(
                new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.ClientId, existingClient.Id)
            );
            var roles = AddExistingRoles(existingClient.TopLevelSecurableItem.Name);
            var addRolesToUserResponse = usersModule.Post($"/user/windows/nouser/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    roles[0]
                });
            }).Result;
            Assert.Equal(HttpStatusCode.NotFound, addRolesToUserResponse.StatusCode);
        }

        [Fact]
        public void AddRolesToUser_MultipleRoles_ReturnsOK()
        {
            var existingClient = ExistingClients.First();
            var usersModule = CreateBrowser(
                new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.ClientId, existingClient.Id)
            );
            var expectedUser = CreateUser(usersModule);
            var newRoles = AddExistingRoles(existingClient.TopLevelSecurableItem.Name);
            var addRolesToUserResponse = usersModule.Post($"/user/{expectedUser.IdentityProvider}/{HttpUtility.UrlEncode(expectedUser.SubjectId)}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    newRoles[0], newRoles[1]
                });
            }).Result;
            Assert.Equal(HttpStatusCode.OK, addRolesToUserResponse.StatusCode);
            var userWithRoles = addRolesToUserResponse.Body.DeserializeJson<UserApiModel>();
            Assert.Equal(2, userWithRoles.Roles.Count);
        }

        [Fact]
        public void AddRolesToUser_EmptyRolesArray_ReturnsBadRequest()
        {
            var existingClient = ExistingClients.First();
            var usersModule = CreateBrowser(
                new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.ClientId, existingClient.Id)
            );
            var expectedUser = CreateUser(usersModule);
            var roles = new List<Role>();
            var addRolesToUserResponse = usersModule.Post($"/user/{expectedUser.IdentityProvider}/{HttpUtility.UrlEncode(expectedUser.SubjectId)}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(roles.ToArray());
            }).Result;
            Assert.Equal(HttpStatusCode.BadRequest, addRolesToUserResponse.StatusCode);
            var error = addRolesToUserResponse.Body.DeserializeJson<Error>();
            Assert.Equal(UsersModule.InvalidRoleArrayMessage, error.Message);

        }

        [Fact]
        public void AddRolesToUser_PostSingleRole_ReturnsBadRequest()
        {
            var existingClient = ExistingClients.First();
            var usersModule = CreateBrowser(
                new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.ClientId, existingClient.Id)
            );
            var expectedUser = CreateUser(usersModule);
            var addRolesToUserResponse = usersModule.Post($"/user/{expectedUser.IdentityProvider}/{HttpUtility.UrlEncode(expectedUser.SubjectId)}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new Role
                {
                    Id = Guid.NewGuid(),
                    Grain = Domain.Defaults.Authorization.AppGrain,
                    SecurableItem = existingClient.TopLevelSecurableItem.Name,
                    Name = "role" + Guid.NewGuid()
                });
            }).Result;
            Assert.Equal(HttpStatusCode.BadRequest, addRolesToUserResponse.StatusCode);
            var error = addRolesToUserResponse.Body.DeserializeJson<Error>();
            Assert.Equal(UsersModule.InvalidRoleArrayMessage, error.Message);
        }

        [Theory]
        [MemberData(nameof(GetBadRoleData))]
        public void AddRolesToUser_InvalidRoleApiModel_ReturnsBadRequest(Role role)
        {
            var existingClient = ExistingClients.First();
            var usersModule = CreateBrowser(
                new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.ClientId, existingClient.Id)
            );
            var expectedUser = CreateUser(usersModule);
            var addRolesToUserResponse = usersModule.Post($"/user/{expectedUser.IdentityProvider}/{HttpUtility.UrlEncode(expectedUser.SubjectId)}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new []
                {
                    role
                });
            }).Result;
            Assert.Equal(HttpStatusCode.BadRequest, addRolesToUserResponse.StatusCode);
            var error = addRolesToUserResponse.Body.DeserializeJson<Error>();
            Assert.Equal(String.Format(UsersModule.InvalidRoleApiModelMessage, role.Name), error.Message);
        }


        [Fact]
        public void AddRolesToUser_RoleAlreadyExistsForUser_ReturnsBadRequest()
        {
            var existingClient = ExistingClients.First();
            var usersModule = CreateBrowser(
                new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.ClientId, existingClient.Id)
            );
            var expectedUser = CreateUser(usersModule);
            var roles = AddExistingRoles(existingClient.TopLevelSecurableItem.Name);
            var addRolesToUserResponse = usersModule.Post($"/user/{expectedUser.IdentityProvider}/{HttpUtility.UrlEncode(expectedUser.SubjectId)}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    roles[0]
                });
            }).Result;
            Assert.Equal(HttpStatusCode.OK, addRolesToUserResponse.StatusCode);

            var addRolesToUserResponseDuplicate = usersModule.Post($"/user/{expectedUser.IdentityProvider}/{HttpUtility.UrlEncode(expectedUser.SubjectId)}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    roles[0]
                });
            }).Result;
            Assert.Equal(HttpStatusCode.BadRequest, addRolesToUserResponseDuplicate.StatusCode);
            var error = addRolesToUserResponseDuplicate.Body.DeserializeJson<Error>();
            Assert.Contains("There was an issue adding roles to the user. Please see the inner exception(s) for details", error.Message);
            Assert.Equal($"The role: {roles[0]} with Id: {roles[0].Id} already exists for the user.", error.Details.First().Message);
            Assert.Single(error.Details);
        }

        [Fact]
        public void AddRolesToUser_MultipleErrors_ReturnsBadRequest()
        {
            var existingClient = ExistingClients.First();
            var usersModule = CreateBrowser(
                new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.ClientId, existingClient.Id)
            );
            var expectedUser = CreateUser(usersModule);
            var roles = AddExistingRoles(existingClient.TopLevelSecurableItem.Name);
            var addRolesToUserResponse = usersModule.Post($"/user/{expectedUser.IdentityProvider}/{HttpUtility.UrlEncode(expectedUser.SubjectId)}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    roles[0]
                });
            }).Result;
            Assert.Equal(HttpStatusCode.OK, addRolesToUserResponse.StatusCode);

            var role = new Role
            {
                Id = Guid.NewGuid(),
                Name = "test" + Guid.NewGuid(),
                Grain = Domain.Defaults.Authorization.AppGrain,
                SecurableItem = existingClient.TopLevelSecurableItem.Name
            };
            var addRolesToUserResponseDuplicate = usersModule.Post($"/user/{expectedUser.IdentityProvider}/{HttpUtility.UrlEncode(expectedUser.SubjectId)}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    roles[0],
                    role
                });
            }).Result;

            Assert.Equal(HttpStatusCode.BadRequest, addRolesToUserResponseDuplicate.StatusCode);
            var error = addRolesToUserResponseDuplicate.Body.DeserializeJson<Error>();
            Assert.Contains("There was an issue adding roles to the user. Please see the inner exception(s) for details", error.Message);
            Assert.Equal($"The role: {roles[0]} with Id: {roles[0].Id} already exists for the user.", error.Details[0].Message);
            Assert.Equal($"The role: {role} with Id: {role.Id} could not be found to add to the user.", error.Details[1].Message);
            Assert.Equal(2, error.Details.Length);
        }

        [Fact]
        public void AddRolesToUser_RoleDoesNotExist_ReturnsBadRequest()
        {
            var existingClient = ExistingClients.First();
            var usersModule = CreateBrowser(
                new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.ClientId, existingClient.Id)
            );
            var expectedUser = CreateUser(usersModule);
            var role = new Role
            {
                Id = Guid.NewGuid(),
                Name = "test" + Guid.NewGuid(),
                Grain = Domain.Defaults.Authorization.AppGrain,
                SecurableItem = existingClient.TopLevelSecurableItem.Name
            };
            var addRolesToUserResponse = usersModule.Post($"/user/{expectedUser.IdentityProvider}/{HttpUtility.UrlEncode(expectedUser.SubjectId)}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    role
                });
            }).Result;

            Assert.Equal(HttpStatusCode.BadRequest, addRolesToUserResponse.StatusCode);
            var error = addRolesToUserResponse.Body.DeserializeJson<Error>();
            Assert.Contains("There was an issue adding roles to the user. Please see the inner exception(s) for details", error.Message);
            Assert.Equal($"The role: {role} with Id: {role.Id} could not be found to add to the user.", error.Details[0].Message);
            Assert.Single(error.Details);
        }

        [Fact]
        public void AddRolesToUser_RoleNotOwnedByCurrentUser_ReturnsForbidden()
        {
            var existingClient = ExistingClients.First();
            var usersModule = CreateBrowser(
                new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.ClientId, existingClient.Id)
            );
            var expectedUser = CreateUser(usersModule);
            var roles = AddExistingRoles(existingClient.TopLevelSecurableItem.Name);
            var role = roles.First(r => r.SecurableItem != existingClient.TopLevelSecurableItem.Name);
            var addRolesToUserResponse = usersModule.Post($"/user/{expectedUser.IdentityProvider}/{HttpUtility.UrlEncode(expectedUser.SubjectId)}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    role
                });
            }).Result;
            Assert.Equal(HttpStatusCode.Forbidden, addRolesToUserResponse.StatusCode);
        }


        private IList<Role> AddExistingRoles(string securableItemName)
        {
            var role1 = new Role
            {
                Id = Guid.NewGuid(),
                Grain = Domain.Defaults.Authorization.AppGrain,
                SecurableItem = securableItemName,
                Name = "role1" + Guid.NewGuid()
            };
            var role2 = new Role
            {
                Id = Guid.NewGuid(),
                Grain = Domain.Defaults.Authorization.AppGrain,
                SecurableItem = securableItemName,
                Name = "role2" + Guid.NewGuid()
            };
            var role3 = new Role
            {
                Id = Guid.NewGuid(),
                Grain = Domain.Defaults.Authorization.AppGrain,
                SecurableItem = "sourcemartdesigner",
                Name = "role3" + Guid.NewGuid()
            };
            ExistingRoles.Add(role1);
            ExistingRoles.Add(role2);
            ExistingRoles.Add(role3);
            return new List<Role>{role1, role2, role3};
        }
        private UserApiModel CreateUser(Browser usersModule)
        {
            var subjectId = "user" + Guid.NewGuid();
            var identityProvider = "Windows";
            var postResponse = usersModule.Post("/user", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    subjectId,
                    identityProvider
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            var result = postResponse.Body.DeserializeJson<UserApiModel>();
            Assert.Equal(subjectId, result.SubjectId);
            Assert.Equal(identityProvider, result.IdentityProvider);
            var selfLink = postResponse.Headers[HttpResponseHeaders.Location];
            Assert.Equal($"{TestHost}/user/{identityProvider}/{subjectId}", selfLink);
            return result;
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
            new object[] {"patientsafety", "badscope"}
        };

        public static IEnumerable<object[]> GetBadRoleData => new[]
        {
            new object[] {new Role {Name = "test", Grain = "test", SecurableItem = "test"}},
            new object[] {new Role {Id = Guid.NewGuid(), SecurableItem = "test", Name = "test"}},
            new object[] {new Role {Id = Guid.NewGuid(), Grain = "test", Name = "test"}},
            new object[] {new Role {Id = Guid.NewGuid(), Name = "test"}},
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
                .Dependency<IPermissionResolverService>(typeof(PermissionResolverService))
                .Dependencies<IPermissionResolverService>(typeof(GranularPermissionResolverService), typeof(RolePermissionResolverService))
                .Dependency(_mockGroupStore.Object)
                .Dependency(_mockUserStore.Object)
                .Dependency(MockLogger.Object)
                .Dependency(MockClientStore.Object)
                .Dependency(MockRoleStore.Object)
                .Dependency(MockPermissionStore.Object)
                .Dependency(MockGrainStore.Object)
                .Dependency(MockSecurableItemStore.Object);
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