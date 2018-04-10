using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.Persistence.SqlServer.Configuration;
using IdentityModel;
using Nancy;
using Nancy.Helpers;
using Nancy.Testing;
using Newtonsoft.Json;
using Xunit;

namespace Fabric.Authorization.IntegrationTests.Modules
{
    [Collection("InMemoryTests")]
    public class UserTests : IClassFixture<IntegrationTestsFixture>
    {
        private static readonly string Group1 = Guid.Parse("A9CA0300-1006-40B1-ABF1-E0C3B396F95F").ToString();
        private static readonly string Source1 = "Source1";

        private static readonly string Group2 = Guid.Parse("ad2cea96-c020-4014-9cf6-029147454adc").ToString();
        private static readonly string Source2 = "Source2";

        private static readonly string IdentityProvider = "idP1";

        private readonly Browser _browser;
        private readonly string _securableItem;

        private readonly string _storageProvider;
        private readonly IntegrationTestsFixture _fixture;

        public UserTests(IntegrationTestsFixture fixture, string storageProvider = StorageProviders.InMemory, ConnectionStrings connectionStrings = null)
        {
            if (connectionStrings != null)
            {
                fixture.ConnectionStrings = connectionStrings;
            }

            _securableItem = "userprincipal" + Guid.NewGuid();
            _storageProvider = storageProvider;
            _fixture = fixture;

            var principal = new ClaimsPrincipal(
                new ClaimsIdentity(new List<Claim>
                {
                    new Claim(Claims.Scope, Scopes.ManageClientsScope),
                    new Claim(Claims.Scope, Scopes.ReadScope),
                    new Claim(Claims.Scope, Scopes.WriteScope),
                    new Claim(Claims.ClientId, _securableItem),
                    new Claim(Claims.Sub, _securableItem),
                    new Claim(JwtClaimTypes.Role, Group1),
                    new Claim(JwtClaimTypes.Role, Group2),
                    new Claim(JwtClaimTypes.IdentityProvider, IdentityProvider)
                }, _securableItem));

            _fixture = fixture;
            _browser = fixture.GetBrowser(principal, storageProvider);
            fixture.CreateClient(_browser, _securableItem);
            Task.Run(async () => await fixture.AssociateUserToAdminRoleAsync(_securableItem, IdentityProvider, storageProvider,
                Domain.Defaults.Authorization.AppGrain, _securableItem, $"{_securableItem}-admin")).Wait();
        }

        [Fact]
        public async Task AddUser_NewUser_ReturnsCreatedAsync()
        {
            var identityProvider = "windows";
            var subjectId = @"domain\test.user" + Guid.NewGuid();
            var response = await _browser.Post("/user", with =>
            {
                with.JsonBody(new
                {
                    identityProvider,
                    subjectId
                });
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var user = response.Body.DeserializeJson<UserApiModel>();
            Assert.Equal(subjectId, user.SubjectId);
            Assert.Equal(identityProvider, user.IdentityProvider);
            Assert.Equal($"{_fixture.TestHost}/user/{identityProvider}/{HttpUtility.UrlEncode(subjectId)}", response.Headers[HttpResponseHeaders.Location]);
        }

        [Fact]
        public async Task AddUser_UserExists_ReturnsConflictAsync()
        {
            var identityProvider = "windows";
            var subjectId = @"domain\test.user" + Guid.NewGuid();
            var response = await _browser.Post("/user", with =>
            {
                with.JsonBody(new
                {
                    identityProvider,
                    subjectId
                });
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var duplicateResponse = await _browser.Post("/user", with =>
            {
                with.JsonBody(new
                {
                    identityProvider,
                    subjectId
                });
            });

            Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
            var error = duplicateResponse.Body.DeserializeJson<Error>();
            Assert.Equal($"The User {subjectId} already exists for the Identity Provider: {identityProvider}", error.Details.First().Message);
        }

        [Fact]
        public async Task AddRolesToUser_ReturnsOkAsync()
        {
            var identityProvider = "windows";
            var subjectId = @"domain\test.user" + Guid.NewGuid();
            var role = await AddUserAndRoleAsync(identityProvider, subjectId);

            var addRoleToUserResponse = await _browser.Post($"/user/{identityProvider}/{HttpUtility.UrlEncode(subjectId)}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new
                    {
                        role.Grain,
                        role.SecurableItem,
                        role.Name,
                        role.Id
                    }
                });
            });

            Assert.Equal(HttpStatusCode.OK, addRoleToUserResponse.StatusCode);
            var updatedUser = addRoleToUserResponse.Body.DeserializeJson<UserApiModel>();
            Assert.Single(updatedUser.Roles);

            var userResponse = await _browser.Get(
                $"/user/{identityProvider}/{HttpUtility.UrlEncode(subjectId)}");
            Assert.Equal(HttpStatusCode.OK, userResponse.StatusCode);
            var userToAssert = userResponse.Body.DeserializeJson<UserApiModel>();
            Assert.Single(userToAssert.Roles);
        }

        [Fact]
        public async Task GetRolesForUser_ReturnsOkAsync()
        {
            var identityProvider = "windows";
            var subjectId = @"domain\test.user" + Guid.NewGuid();
            var role = await AddUserAndRoleAsync(identityProvider, subjectId);

            var addRoleToUserResponse = await _browser.Post($"/user/{identityProvider}/{HttpUtility.UrlEncode(subjectId)}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new
                    {
                        role.Grain,
                        role.SecurableItem,
                        role.Name,
                        role.Id
                    }
                });
            });

            Assert.Equal(HttpStatusCode.OK, addRoleToUserResponse.StatusCode);
            var updatedUser = addRoleToUserResponse.Body.DeserializeJson<UserApiModel>();
            Assert.Single(updatedUser.Roles);

            var userResponse = await _browser.Get(
                $"/user/{identityProvider}/{HttpUtility.UrlEncode(subjectId)}");
            Assert.Equal(HttpStatusCode.OK, userResponse.StatusCode);
            var userToAssert = userResponse.Body.DeserializeJson<UserApiModel>();
            Assert.Single(userToAssert.Roles);

            var rolesResponse =
                await _browser.Get($"/user/{identityProvider}/{HttpUtility.UrlEncode(subjectId)}/roles");
            Assert.Equal(HttpStatusCode.OK, rolesResponse.StatusCode);
            Assert.Single(rolesResponse.Body.DeserializeJson<List<RoleApiModel>>());
        }

        [Fact]
        public async Task DeleteRolesFromUser_ReturnsOKAsync()
        {
            var identityProvider = "windows";
            var subjectId = @"domain\test.user" + Guid.NewGuid();
            var role = await AddUserAndRoleAsync(identityProvider, subjectId);

            var addRoleToUserResponse = await _browser.Post($"/user/{identityProvider}/{HttpUtility.UrlEncode(subjectId)}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new
                    {
                        role.Grain,
                        role.SecurableItem,
                        role.Name,
                        role.Id
                    }
                });
            });

            Assert.Equal(HttpStatusCode.OK, addRoleToUserResponse.StatusCode);

            var userResponse = await _browser.Get(
                $"/user/{identityProvider}/{HttpUtility.UrlEncode(subjectId)}");
            Assert.Equal(HttpStatusCode.OK, userResponse.StatusCode);
            var userToAssert = userResponse.Body.DeserializeJson<UserApiModel>();
            Assert.Single(userToAssert.Roles);

            var deleteRoleFromUserResponse = await _browser.Delete(
                $"/user/{identityProvider}/{HttpUtility.UrlEncode(subjectId)}/roles",
                with =>
                {
                    with.HttpRequest();
                    with.JsonBody(new []
                    {
                        role
                    });
                });

            Assert.Equal(HttpStatusCode.OK, deleteRoleFromUserResponse.StatusCode);
            var updatedUser = deleteRoleFromUserResponse.Body.DeserializeJson<UserApiModel>();
            Assert.Empty(updatedUser.Roles);

            userResponse = await _browser.Get(
                $"/user/{identityProvider}/{HttpUtility.UrlEncode(subjectId)}");
            Assert.Equal(HttpStatusCode.OK, userResponse.StatusCode);
            userToAssert = userResponse.Body.DeserializeJson<UserApiModel>();
            Assert.Empty(userToAssert.Roles);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task AddRole_DeleteRole_AddRoleAgain_SuccessAsync()
        {
            var identityProvider = "windows";
            var subjectId = @"domain\test.user" + Guid.NewGuid();
            var role = await AddUserAndRoleAsync(identityProvider, subjectId);

            var addRoleToUserResponse = await _browser.Post($"/user/{identityProvider}/{HttpUtility.UrlEncode(subjectId)}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new
                    {
                        role.Grain,
                        role.SecurableItem,
                        role.Name,
                        role.Id
                    }
                });
            });

            Assert.Equal(HttpStatusCode.OK, addRoleToUserResponse.StatusCode);

            var userResponse = await _browser.Get(
                $"/user/{identityProvider}/{HttpUtility.UrlEncode(subjectId)}");
            Assert.Equal(HttpStatusCode.OK, userResponse.StatusCode);
            var userToAssert = userResponse.Body.DeserializeJson<UserApiModel>();
            Assert.Single(userToAssert.Roles);

            var deleteRoleFromUserResponse = await _browser.Delete(
                $"/user/{identityProvider}/{HttpUtility.UrlEncode(subjectId)}/roles",
                with =>
                {
                    with.HttpRequest();
                    with.JsonBody(new[]
                    {
                        role
                    });
                });

            Assert.Equal(HttpStatusCode.OK, deleteRoleFromUserResponse.StatusCode);
            var updatedUser = deleteRoleFromUserResponse.Body.DeserializeJson<UserApiModel>();
            Assert.Empty(updatedUser.Roles);

            userResponse = await _browser.Get(
                $"/user/{identityProvider}/{HttpUtility.UrlEncode(subjectId)}");
            Assert.Equal(HttpStatusCode.OK, userResponse.StatusCode);
            userToAssert = userResponse.Body.DeserializeJson<UserApiModel>();
            Assert.Empty(userToAssert.Roles);

            //add the same role to the user
            addRoleToUserResponse = await _browser.Post($"/user/{identityProvider}/{HttpUtility.UrlEncode(subjectId)}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new
                    {
                        role.Grain,
                        role.SecurableItem,
                        role.Name,
                        role.Id
                    }
                });
            });

            Assert.Equal(HttpStatusCode.OK, addRoleToUserResponse.StatusCode);

            userResponse = await _browser.Get(
                $"/user/{identityProvider}/{HttpUtility.UrlEncode(subjectId)}");
            Assert.Equal(HttpStatusCode.OK, userResponse.StatusCode);
            userToAssert = userResponse.Body.DeserializeJson<UserApiModel>();
            Assert.Single(userToAssert.Roles);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task GetUserPermissions_SharedGrain_SuccessAsync()
        {
            var clientId = Domain.Defaults.Authorization.InstallerClientId;
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(Claims.Scope, Scopes.ManageClientsScope),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.Scope, Scopes.ManageDosScope),
                new Claim(Claims.ClientId, clientId)
            }, "pwd"));

            var browser = _fixture.GetBrowser(principal, _storageProvider);

            var permission = "permission" + Guid.NewGuid();
            var post = await browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "dos",
                    SecurableItem = "datamarts",
                    Name = permission
                });
            });

            Assert.Equal(HttpStatusCode.Created, post.StatusCode);
            var dosDatamartPermission = JsonConvert.DeserializeObject<PermissionApiModel>(post.Body.AsString());

            post = await _browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Id = "DosGroup",
                    GroupName = "DosGroup",
                    GroupSource = "Custom"
                });
            });

            Assert.Equal(HttpStatusCode.Created, post.StatusCode); 

            post = await browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new RoleApiModel
                {
                    Grain = "dos",
                    SecurableItem = "datamarts",
                    Name = $"datamartrole-{Guid.NewGuid()}",
                    Permissions = new List<PermissionApiModel> { dosDatamartPermission }
                });
            });

            Assert.Equal(HttpStatusCode.Created, post.StatusCode);

            var dosDatamartRole = post.Body.DeserializeJson<RoleApiModel>();

            post = await browser.Post("/groups/DosGroup/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new
                    {
                        dosDatamartRole.Grain,
                        dosDatamartRole.SecurableItem,
                        dosDatamartRole.Name,
                        dosDatamartRole.Id
                    }
                });
            });

            Assert.Equal(HttpStatusCode.OK, post.StatusCode);

            var subjectId = "bob.smith";
            var idP = "Windows";

            // add user to group
            post = await browser.Post("/groups/DosGroup/users", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new
                    {
                        SubjectId = subjectId,
                        IdentityProvider = idP
                    }
                });
            });

            Assert.Equal(HttpStatusCode.OK, post.StatusCode);

            // get non-authenticated user's permissions
            var get = await _browser.Get($"/user/{idP}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            var permissions = get.Body.DeserializeJson<IEnumerable<ResolvedPermissionApiModel>>();
            var permissionNames = permissions.Select(p => p.ToString());
            Assert.Contains($"dos/datamarts.{dosDatamartPermission.Name}", permissionNames);

            // create a principal w/ the user created above
            principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(Claims.Scope, Scopes.ManageClientsScope),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.Sub, subjectId),
                new Claim(Claims.IdentityProvider, idP),
                new Claim(Claims.ClientId, _securableItem)
            }, "pwd"));

            browser = _fixture.GetBrowser(principal, _storageProvider);

            get = await browser.Get("/user/permissions", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            var userPermissionsApiModel = get.Body.DeserializeJson<UserPermissionsApiModel>();
            Assert.Contains($"dos/datamarts.{dosDatamartPermission.Name}", userPermissionsApiModel.Permissions);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task GetUserPermissions_NonAuthenticatedUserWithPermissions_SuccessAsync()
        {
            var groupName = "Admin" + Guid.NewGuid();
            var roleName = "Administrator" + Guid.NewGuid();
            var permissionNames = new[] { "viewpatients" + Guid.NewGuid(), "editpatients" + Guid.NewGuid(), "adminpatients" + Guid.NewGuid(), "deletepatients" + Guid.NewGuid() };

            const string subjectId = "first.last";
            const string identityProvider = "Windows";

            // add custom group
            var response = await _browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new GroupRoleApiModel
                {
                    GroupName = groupName,
                    GroupSource = "Custom"
                });
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // add role
            response = await _browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new RoleApiModel
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = roleName
                });
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var role = response.Body.DeserializeJson<RoleApiModel>();
            var roleId = role.Id;

            // add role to group
            response = await _browser.Post($"/groups/{groupName}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new
                    {
                        role.Grain,
                        role.SecurableItem,
                        role.Name,
                        role.Id
                    }
                });
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // add 4 permissions
            response = await _browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = permissionNames[0]
                });
            });

            var permission1Id = response.Body.DeserializeJson<PermissionApiModel>().Id;

            response = await _browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = permissionNames[1]
                });
            });

            var permission2Id = response.Body.DeserializeJson<PermissionApiModel>().Id;

            response = await _browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = permissionNames[2]
                });
            });

            var permission3Id = response.Body.DeserializeJson<PermissionApiModel>().Id;

            response = await _browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = permissionNames[3]
                });
            });

            var permission4Id = response.Body.DeserializeJson<PermissionApiModel>().Id;

            // add user to group
            response = await _browser.Post($"/groups/{groupName}/users", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new
                    {
                        SubjectId = subjectId,
                        IdentityProvider = identityProvider
                    }
                });
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var permissionApiModels = new List<PermissionApiModel>
            {
                new PermissionApiModel
                {
                    Id = permission1Id,
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = permissionNames[0],
                    PermissionAction = PermissionAction.Allow
                },
                new PermissionApiModel
                {
                    Id = permission2Id,
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = permissionNames[1],
                    PermissionAction = PermissionAction.Deny
                },
                new PermissionApiModel
                {
                    Id = permission3Id,
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = permissionNames[2],
                    PermissionAction = PermissionAction.Allow
                },
                new PermissionApiModel
                {
                    Id = permission4Id,
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = permissionNames[3],
                    PermissionAction = PermissionAction.Deny
                }
            };

            // create 2 role-based permissions
            response = await _browser.Post($"/roles/{roleId}/permissions", with =>
            {
                with.HttpRequest();

                with.Body(JsonConvert.SerializeObject(
                    new List<PermissionApiModel> { permissionApiModels[0], permissionApiModels[1] }));

            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // create 2 granular (user-based) permissions
            response = await _browser.Post($"/user/{identityProvider}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();

                with.Body(JsonConvert.SerializeObject(
                    new List<PermissionApiModel> { permissionApiModels[2], permissionApiModels[3] }));

            });

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // retrieve permissions for user
            response = await _browser.Get($"/user/{identityProvider}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();
            });

            var permissions = response.Body.DeserializeJson<List<ResolvedPermissionApiModel>>();

            Assert.NotNull(permissions);
            Assert.Equal(4, permissions.Count);

            var permission1 = permissions.FirstOrDefault(p => p.Name == permissionNames[0]);
            Assert.NotNull(permission1);
            Assert.Equal(PermissionAction.Allow, permission1.PermissionAction);
            Assert.Single(permission1.Roles);

            var permission3 = permissions.FirstOrDefault(p => p.Name == permissionNames[2]);
            Assert.NotNull(permission3);
            Assert.Equal(PermissionAction.Allow, permission3.PermissionAction);
            Assert.Empty(permission3.Roles);
            Assert.NotEqual(DateTime.MinValue, permission3.CreatedDateTimeUtc);

            var permission4 = permissions.FirstOrDefault(p => p.Name == permissionNames[3]);
            Assert.NotNull(permission4);
            Assert.Equal(PermissionAction.Deny, permission4.PermissionAction);
            Assert.Empty(permission4.Roles);
            Assert.NotEqual(DateTime.MinValue, permission4.CreatedDateTimeUtc);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task Test_AddGranularPermission_AllowDenyPermissionInSameRequestAsync()
        {
            var permissionName = "readpatient" + Guid.NewGuid();
            var allowReadPatientPermission = new PermissionApiModel
            {
                Grain = "app",
                SecurableItem = _securableItem,
                Name = permissionName,
                PermissionAction = PermissionAction.Allow
            };

            var denyReadPatientPermission = new PermissionApiModel
            {
                Grain = "app",
                SecurableItem = _securableItem,
                Name = permissionName,
                PermissionAction = PermissionAction.Deny
            };

            var response = await _browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(allowReadPatientPermission);
            });

            allowReadPatientPermission.Id = response.Body.DeserializeJson<PermissionApiModel>().Id;
            denyReadPatientPermission.Id = allowReadPatientPermission.Id;

            string subjectId = _securableItem;

            var postResponse = await _browser.Post($"/user/{IdentityProvider}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();
                var perms = new List<PermissionApiModel> { allowReadPatientPermission, denyReadPatientPermission };
                with.JsonBody(perms);
            });

            Assert.Equal(HttpStatusCode.Conflict, postResponse.StatusCode);
            Assert.Contains($"The following permissions cannot be specified as both 'allow' and 'deny': app/{subjectId}.{permissionName}", postResponse.Body.AsString());
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task Test_AddGranularPermission_DuplicateAsync()
        {
            var modifyPatientPermission = new PermissionApiModel
            {
                Grain = "app",
                SecurableItem = _securableItem,
                Name = "modifypatient" + Guid.NewGuid(),
                PermissionAction = PermissionAction.Allow
            };

            var response = await _browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(modifyPatientPermission);
            });

            modifyPatientPermission.Id = response.Body.DeserializeJson<PermissionApiModel>().Id;

            var subjectId = _securableItem;

            await _browser.Post($"/user/{IdentityProvider}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();
                var perms = new List<PermissionApiModel> { modifyPatientPermission };
                with.JsonBody(perms);
            });

            var postResponse = await _browser.Post($"/user/{IdentityProvider}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();
                var perms = new List<PermissionApiModel> { modifyPatientPermission };
                with.JsonBody(perms);
            });

            Assert.Equal(HttpStatusCode.Conflict, postResponse.StatusCode);
            Assert.Contains(
                $"The following permissions already exist as 'allow' permissions: app/{subjectId}.{modifyPatientPermission.Name}",
                postResponse.Body.AsString());
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task Test_AddGranularPermission_ExistWithOtherAction_DuplicateAsync()
        {
            var modifyPatientPermission = new PermissionApiModel
            {
                Grain = "app",
                SecurableItem = _securableItem,
                Name = "modifypatient" + Guid.NewGuid(),
                PermissionAction = PermissionAction.Allow
            };

            var deletePatientPermission = new PermissionApiModel
            {
                Grain = "app",
                SecurableItem = _securableItem,
                Name = "deletepatient" + Guid.NewGuid(),
                PermissionAction = PermissionAction.Allow
            };

            var readPatientPermission = new PermissionApiModel
            {
                Grain = "app",
                SecurableItem = _securableItem,
                Name = "readpatient" + Guid.NewGuid(),
                PermissionAction = PermissionAction.Allow
            };

            var response = await _browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(modifyPatientPermission);
            });

            modifyPatientPermission.Id = response.Body.DeserializeJson<PermissionApiModel>().Id;

            response = await _browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(deletePatientPermission);
            });

            deletePatientPermission.Id = response.Body.DeserializeJson<PermissionApiModel>().Id;

            response = await _browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(readPatientPermission);
            });

            readPatientPermission.Id = response.Body.DeserializeJson<PermissionApiModel>().Id;

            var subjectId = _securableItem;

            await _browser.Post($"/user/{IdentityProvider}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();
                var perms = new List<PermissionApiModel>
                {
                    modifyPatientPermission,
                    deletePatientPermission,
                    readPatientPermission
                };
                with.JsonBody(perms);
            });

            modifyPatientPermission.PermissionAction = PermissionAction.Deny;

            var postResponse = await _browser.Post($"/user/{IdentityProvider}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();
                var perms = new List<PermissionApiModel>
                {
                    modifyPatientPermission,
                    deletePatientPermission,
                    readPatientPermission
                };
                with.JsonBody(perms);
            });

            Assert.Equal(HttpStatusCode.Conflict, postResponse.StatusCode);
            Assert.Contains(
                $"The following permissions exist as 'allow' and cannot be added as 'deny': app/{_securableItem}.{modifyPatientPermission.Name}",
                postResponse.Body.AsString());
            Assert.Contains(
                $"The following permissions already exist as 'allow' permissions: app/{_securableItem}.{deletePatientPermission.Name}, app/{_securableItem}.{readPatientPermission.Name}",
                postResponse.Body.AsString());
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task Test_AddGranularPermissions_NoPermissionsInBodyAsync()
        {
            var subjectId = _securableItem;

            var postRequest = await _browser.Post($"/user/{IdentityProvider}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();
                var perms = new List<PermissionApiModel>();
                with.JsonBody(perms);
            });

            Assert.Equal(HttpStatusCode.BadRequest, postRequest.StatusCode);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task Test_AddGranularPermssion_ExistsWithOtherActionAsync()
        {
            var modifyPatientPermission = new PermissionApiModel
            {
                Grain = "app",
                SecurableItem = _securableItem,
                Name = "modifypatient" + Guid.NewGuid(),
                PermissionAction = PermissionAction.Allow
            };

            var response = await _browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(modifyPatientPermission);
            });

            modifyPatientPermission.Id = response.Body.DeserializeJson<PermissionApiModel>().Id;

            var subjectId = _securableItem;

            await _browser.Post($"/user/{IdentityProvider}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();
                var perms = new List<PermissionApiModel> { modifyPatientPermission };
                with.JsonBody(perms);
            });

            modifyPatientPermission.PermissionAction = PermissionAction.Deny;

            var postResponse = await _browser.Post($"/user/{IdentityProvider}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();
                var perms = new List<PermissionApiModel> { modifyPatientPermission };
                with.JsonBody(perms);
            });

            Assert.Equal(HttpStatusCode.Conflict, postResponse.StatusCode);
            Assert.Contains(
                $"The following permissions exist as 'allow' and cannot be added as 'deny': app/{_securableItem}.{modifyPatientPermission.Name}",
                postResponse.Body.AsString());
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task Test_Delete_SuccessAsync()
        {
            // Adding permission
            var modifyPatientPermission = new PermissionApiModel
            {
                Grain = "app",
                SecurableItem = _securableItem,
                Name = "modifypatient" + Guid.NewGuid(),
                PermissionAction = PermissionAction.Allow
            };

            var response = await _browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(modifyPatientPermission);
            });

            modifyPatientPermission.Id = response.Body.DeserializeJson<PermissionApiModel>().Id;

            var subjectId = _securableItem;
            await _browser.Post($"/user/{IdentityProvider}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();
                var perms = new List<PermissionApiModel> { modifyPatientPermission };
                with.JsonBody(perms);
            });

            // Get the permissions
            var get = await _browser.Get("/user/permissions", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            var permissions = get.Body.DeserializeJson<UserPermissionsApiModel>();
            Assert.Contains($"app/{_securableItem}.{modifyPatientPermission.Name}", permissions.Permissions);

            //delete the permission
            await _browser.Delete($"/user/{IdentityProvider}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();
                var perms = new List<PermissionApiModel> { modifyPatientPermission };
                with.JsonBody(perms);
            });

            // Get the permissions
            get = await _browser.Get("/user/permissions", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            permissions = get.Body.DeserializeJson<UserPermissionsApiModel>();
            Assert.DoesNotContain("app/{_securableItem}.{modifyPatientPermission.Name}", permissions.Permissions);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task Test_Delete_UserHasNoGranularPermissionsAsync()
        {
            // Get the permissions
            var get = await _browser.Get("/user/permissions", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            var permissions = get.Body.DeserializeJson<UserPermissionsApiModel>();
            Assert.Single(permissions.Permissions);

            var modifyPatientPermission = new PermissionApiModel
            {
                Grain = "app",
                SecurableItem = _securableItem,
                Name = "modifypatient" + Guid.NewGuid(),
                PermissionAction = PermissionAction.Allow
            };

            var subjectId = _securableItem;

            //attempt to delete a permission the user does not have 
            var deleteRequest = await _browser.Delete($"/user/{IdentityProvider}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();
                var perms = new List<PermissionApiModel> { modifyPatientPermission };
                with.JsonBody(perms);
            });

            Assert.Equal(HttpStatusCode.BadRequest, deleteRequest.StatusCode);
            Assert.Contains(
                $"The following permissions do not exist as 'allow' permissions: app/{_securableItem}.{modifyPatientPermission.Name}",
                deleteRequest.Body.AsString());
            Assert.DoesNotContain("The following permissions exist as 'deny' for user but 'allow' was specified",
                deleteRequest.Body.AsString());
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task Test_Delete_WrongPermissionActionAsync()
        {
            var modifyPatientPermission = new PermissionApiModel
            {
                Grain = "app",
                SecurableItem = _securableItem,
                Name = "modifypatient" + Guid.NewGuid(),
                PermissionAction = PermissionAction.Allow
            };

            var response = await _browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(modifyPatientPermission);
            });

            modifyPatientPermission.Id = response.Body.DeserializeJson<PermissionApiModel>().Id;

            var subjectId = _securableItem;

            await _browser.Post($"/user/{IdentityProvider}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();
                var perms = new List<PermissionApiModel> { modifyPatientPermission };
                with.JsonBody(perms);
            });

            // Get the permissions
            var get = await _browser.Get("/user/permissions", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            var permissions = get.Body.DeserializeJson<UserPermissionsApiModel>();
            Assert.Contains($"app/{subjectId}.{modifyPatientPermission.Name}", permissions.Permissions);

            //attempt to delete modifyPatientPermission with permission action Deny
            modifyPatientPermission.PermissionAction = PermissionAction.Deny;

            var deleteRequest = await _browser.Delete($"/user/{IdentityProvider}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();
                var perms = new List<PermissionApiModel> { modifyPatientPermission };
                with.JsonBody(perms);
            });

            Assert.Equal(HttpStatusCode.BadRequest, deleteRequest.StatusCode);
            Assert.Contains(
                $"The following permissions exist as 'allow' for user but 'deny' was specified: app/{subjectId}.{modifyPatientPermission.Name}",
                deleteRequest.Body.AsString());
            Assert.DoesNotContain("The permissions do not exist as 'deny' permissions", deleteRequest.Body.AsString());
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task Test_Delete_WrongPermissionAction_InvalidPermissionAsync()
        {
            var modifyPatientPermission = new PermissionApiModel
            {
                Grain = "app",
                SecurableItem = _securableItem,
                Name = "modifypatient" + Guid.NewGuid(),
                PermissionAction = PermissionAction.Allow
            };

            var response = await _browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(modifyPatientPermission);
            });

            modifyPatientPermission.Id = response.Body.DeserializeJson<PermissionApiModel>().Id;

            var subjectId = _securableItem;

            await _browser.Post($"/user/{IdentityProvider}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();
                var perms = new List<PermissionApiModel> { modifyPatientPermission };
                with.JsonBody(perms);
            });

            // Get the permissions
            var get = await _browser.Get("/user/permissions", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            var permissions = get.Body.DeserializeJson<UserPermissionsApiModel>();
            Assert.Contains($"app/{_securableItem}.{modifyPatientPermission.Name}", permissions.Permissions);

            //attempt to delete modifyPatientPermission with permission action Deny and include an invalid permission
            modifyPatientPermission.PermissionAction = PermissionAction.Deny;

            var deletePatientPermission = new PermissionApiModel
            {
                Grain = "app",
                SecurableItem = _securableItem,
                Name = "deletepatient" + Guid.NewGuid(),
                PermissionAction = PermissionAction.Allow
            };

            var deleteRequest = await _browser.Delete($"/user/{IdentityProvider}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();
                var perms = new List<PermissionApiModel> { modifyPatientPermission, deletePatientPermission };
                with.JsonBody(perms);
            });

            Assert.Equal(HttpStatusCode.BadRequest, deleteRequest.StatusCode);
            Assert.Contains(
                $"The following permissions exist as 'allow' for user but 'deny' was specified: app/{subjectId}.{modifyPatientPermission.Name}",
                deleteRequest.Body.AsString());
            Assert.DoesNotContain("The permissions do not exist as 'deny' permissions", deleteRequest.Body.AsString());
            Assert.Contains(
                $"The following permissions do not exist as 'allow' permissions: app/{subjectId}.{deletePatientPermission.Name}",
                deleteRequest.Body.AsString());
            Assert.DoesNotContain("The following permissions exist as 'deny' for user but 'allow' was specified",
                deleteRequest.Body.AsString());
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task Test_DeleteGranularPermissions_NoPermissionsInBodyAsync()
        {
            var subjectId = _securableItem;

            var deleteRequest = await _browser.Delete($"/user/{IdentityProvider}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();
                var perms = new List<PermissionApiModel>();
                with.JsonBody(perms);
            });

            Assert.Equal(HttpStatusCode.BadRequest, deleteRequest.StatusCode);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task TestGetPermissions_SuccessAsync()
        {
            // Adding permissions
            var post = await _browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = "viewpatient" + Guid.NewGuid()
                });
            });

            var viewPatientPermission = post.Body.DeserializeJson<PermissionApiModel>();

            post = await _browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = "editpatient" + Guid.NewGuid()
                });
            });

            var editPatientPermission = post.Body.DeserializeJson<PermissionApiModel>();

            // create roles
            var role = new RoleApiModel
            {
                Grain = "app",
                SecurableItem = _securableItem,
                Name = "viewer" + Guid.NewGuid(),
                Permissions = new List<PermissionApiModel> { viewPatientPermission }
            };

            post = await _browser.Post("/roles", with => // -3
            {
                with.HttpRequest();
                with.JsonBody(role);
            });

            var viewerRole = post.Body.DeserializeJson<RoleApiModel>();

            post = await _browser.Post("/roles", with => // -2
            {
                with.HttpRequest();
                role.Name = "editor" + Guid.NewGuid();
                role.Permissions = new List<PermissionApiModel> { editPatientPermission };
                with.JsonBody(role);
            });

            var editorRole = post.Body.DeserializeJson<RoleApiModel>();
            await _browser.Post($"/roles/{viewerRole.Id}/permissions", with =>
                {
                    with.HttpRequest();
                    with.JsonBody(new[]
                    {
                        viewPatientPermission
                    });
                });

            await _browser.Post($"/roles/{editorRole.Id}/permissions", with =>
                {
                    with.HttpRequest();
                    with.JsonBody(new[]
                    {
                        editPatientPermission
                    });
                });

            await _browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Id = Group1,
                    GroupName = Group1,
                    GroupSource = Source1
                });
            });

            await _browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Id = Group2,
                    GroupName = Group2,
                    GroupSource = Source2
                });
            });

            await _browser.Post($"/groups/{Group1}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new
                    {
                        viewerRole.Grain,
                        viewerRole.SecurableItem,
                        viewerRole.Name,
                        viewerRole.Id
                    }
                });
            });

            await _browser.Post($"/groups/{Group2}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new
                    {
                        editorRole.Grain,
                        editorRole.SecurableItem,
                        editorRole.Name,
                        editorRole.Id
                    }
                });
            });

            // Get the permissions
            var get = await _browser.Get("/user/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            });

            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            var permissions = get.Body.DeserializeJson<UserPermissionsApiModel>();
            Assert.Contains($"app/{_securableItem}.{editPatientPermission.Name}", permissions.Permissions);
            Assert.Contains($"app/{_securableItem}.{viewPatientPermission.Name}", permissions.Permissions);
            Assert.Equal(3, permissions.Permissions.Count());
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task TestInheritance_SuccessAsync()
        {
            var group = Group1;

            // Adding permissions
            var post = await _browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = "greatgrandfatherpermissions" + Guid.NewGuid()
                });
            });

            var ggfperm = post.Body.DeserializeJson<PermissionApiModel>();

            post = await _browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = "grandfatherpermissions" + Guid.NewGuid()
                });
            });

            var gfperm = post.Body.DeserializeJson<PermissionApiModel>();

            post = await _browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = "fatherpermissions" + Guid.NewGuid()
                });
            });

            var fperm = post.Body.DeserializeJson<PermissionApiModel>();

            post = await _browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = "himselfpermissions" + Guid.NewGuid()
                });
            });

            var hsperm = post.Body.DeserializeJson<PermissionApiModel>();

            post = await _browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = "sonpermissions" + Guid.NewGuid()
                });
            });

            var sonperm = post.Body.DeserializeJson<PermissionApiModel>();

            // Adding Roles
            var role = new RoleApiModel
            {
                Grain = "app",
                SecurableItem = _securableItem,
                Name = "greatgrandfather" + Guid.NewGuid(),
                Permissions = new List<PermissionApiModel> { ggfperm }
            };

            post = await _browser.Post("/roles", with => // -3
            {
                with.HttpRequest();
                with.JsonBody(role);
            });

            var ggf = post.Body.DeserializeJson<RoleApiModel>();

            post = await _browser.Post("/roles", with => // -2
            {
                with.HttpRequest();
                role.Name = "grandfather" + Guid.NewGuid();
                role.ParentRole = ggf.Id;
                role.Permissions = new List<PermissionApiModel> { gfperm };
                with.JsonBody(role);
            });

            var gf = post.Body.DeserializeJson<RoleApiModel>();

            post = await _browser.Post("/roles", with => // -1
            {
                with.HttpRequest();
                role.Name = "father" + Guid.NewGuid();
                role.ParentRole = gf.Id;
                role.Permissions = new List<PermissionApiModel> { fperm };
                with.JsonBody(role);
            });

            var f = post.Body.DeserializeJson<RoleApiModel>();

            post = await _browser.Post("/roles", with => // 0
            {
                with.HttpRequest();
                role.Name = "himself" + Guid.NewGuid();
                role.ParentRole = f.Id;
                role.Permissions = new List<PermissionApiModel> { hsperm };
                with.JsonBody(role);
            });

            var hs = post.Body.DeserializeJson<RoleApiModel>();

            post = await _browser.Post("/roles", with => // 1
            {
                with.HttpRequest();
                role.Name = "son" + Guid.NewGuid();
                role.ParentRole = hs.Id;
                role.Permissions = new List<PermissionApiModel> { sonperm };
                with.JsonBody(role);
            });

            post.Body.DeserializeJson<RoleApiModel>();

            // Adding groups
            await _browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Id = group,
                    GroupName = group,
                    GroupSource = Source1
                });
            });

            await _browser.Post($"/groups/{group}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new
                    {
                        hs.Grain,
                        hs.SecurableItem,
                        hs.Name,
                        hs.Id
                    }
                });
            });

            // Get the permissions
            var get = await _browser.Get("/user/permissions", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            Assert.Contains(ggfperm.Name, get.Body.AsString());
            Assert.Contains(gfperm.Name, get.Body.AsString());
            Assert.Contains(fperm.Name, get.Body.AsString());
            Assert.Contains(hsperm.Name, get.Body.AsString());
            Assert.DoesNotContain(sonperm.Name, get.Body.AsString());
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task TestRoleBlacklist_SuccessAsync()
        {
            // Adding permissions
            var post = await _browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = "viewpatient" + Guid.NewGuid()
                });
            });

            var viewPatientPermission = post.Body.DeserializeJson<PermissionApiModel>();

            post = await _browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = "editpatient" + Guid.NewGuid()
                });
            });

            var editPatientPermission = post.Body.DeserializeJson<PermissionApiModel>();

            // Adding roles
            var role = new RoleApiModel
            {
                Grain = "app",
                SecurableItem = _securableItem,
                Name = "viewer" + Guid.NewGuid(),
                Permissions = new List<PermissionApiModel> { viewPatientPermission }
            };

            post = await _browser.Post("/roles", with => // -3
            {
                with.HttpRequest();
                with.JsonBody(role);
            });

            var viewerRole = post.Body.DeserializeJson<RoleApiModel>();

            post = await _browser.Post("/roles", with => // -2
            {
                with.HttpRequest();
                role.Name = "editor" + Guid.NewGuid();
                role.Permissions = new List<PermissionApiModel> { editPatientPermission };

                // Role denies viewPatient permission
                role.DeniedPermissions = new List<PermissionApiModel> { viewPatientPermission };
                with.JsonBody(role);
            });

            var editorRole = post.Body.DeserializeJson<RoleApiModel>();

            // Adding groups
            await _browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Id = Group1,
                    GroupName = Group1,
                    GroupSource = Source1
                });
            });

            await _browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Id = Group2,
                    GroupName = Group2,
                    GroupSource = Source2
                });
            });

            await _browser.Post($"/groups/{Group1}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new
                    {
                        viewerRole.Grain,
                        viewerRole.SecurableItem,
                        viewerRole.Name,
                        viewerRole.Id
                    }
                });
            });

            await _browser.Post($"/groups/{Group2}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new
                    {
                        editorRole.Grain,
                        editorRole.SecurableItem,
                        editorRole.Name,
                        editorRole.Id
                    }
                });
            });

            // Get the permissions
            var get = await _browser.Get("/user/permissions", with =>
            {
                with.HttpRequest();
            });

            var subjectId = _securableItem;

            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            var permissions = get.Body.DeserializeJson<UserPermissionsApiModel>();
            Assert.Contains($"app/{subjectId}.{editPatientPermission.Name}", permissions.Permissions);
            Assert.DoesNotContain($"app/{subjectId}.{viewPatientPermission.Name}", permissions.Permissions); // Denied by role
            Assert.Equal(2, permissions.Permissions.Count());
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task TestUserBlacklist_SuccessAsync()
        {
            // Adding permissions
            var post = await _browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = "viewpatient" + Guid.NewGuid()
                });
            });

            var viewPatientPermission = post.Body.DeserializeJson<PermissionApiModel>();

            post = await _browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = "editpatient" + Guid.NewGuid()
                });
            });

            var editPatientPermission = post.Body.DeserializeJson<PermissionApiModel>();

            // Adding roles
            var role = new RoleApiModel
            {
                Grain = "app",
                SecurableItem = _securableItem,
                Name = "viewer" + Guid.NewGuid(),
                Permissions = new List<PermissionApiModel> { viewPatientPermission }
            };

            post = await _browser.Post("/roles", with => // -3
            {
                with.HttpRequest();
                with.JsonBody(role);
            });

            var viewerRole = post.Body.DeserializeJson<RoleApiModel>();

            post = await _browser.Post("/roles", with => // -2
            {
                with.HttpRequest();
                role.Name = "editor" + Guid.NewGuid();
                role.Permissions = new List<PermissionApiModel> { editPatientPermission };
                with.JsonBody(role);
            });

            var editorRole = post.Body.DeserializeJson<RoleApiModel>();

            // Adding groups
            await _browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Id = Group1,
                    GroupName = Group1,
                    GroupSource = Source1
                });
            });

            await _browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Id = Group2,
                    GroupName = Group2,
                    GroupSource = Source2
                });
            });

            await _browser.Post($"/groups/{Group1}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new
                    {
                        viewerRole.Grain,
                        viewerRole.SecurableItem,
                        viewerRole.Name,
                        viewerRole.Id
                    }
                });
            });

            await _browser.Post($"/groups/{Group2}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new
                    {
                        editorRole.Grain,
                        editorRole.SecurableItem,
                        editorRole.Name,
                        editorRole.Id
                    }
                });
            });

            // Adding blacklist (user cannot edit patient, even though role allows)
            var subjectId = _securableItem;

            editPatientPermission.PermissionAction = PermissionAction.Deny;

            await _browser.Post($"/user/{IdentityProvider}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();
                var perms = new List<PermissionApiModel> { editPatientPermission };
                with.JsonBody(perms);
            });

            // Get the permissions
            var get = await _browser.Get("/user/permissions", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            var permissions = get.Body.DeserializeJson<UserPermissionsApiModel>();
            Assert.DoesNotContain($"app/{_securableItem}.{editPatientPermission.Name}", permissions.Permissions);
            Assert.Contains($"app/{_securableItem}.{viewPatientPermission.Name}", permissions.Permissions);
            Assert.Equal(2, permissions.Permissions.Count());
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodNameAttribute]
        public async Task TestUserWhitelist_SuccessAsync()
        {
            // Adding permissions
            var post = await _browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = "viewpatient" + Guid.NewGuid()
                });
            });

            var viewPatientPermission = post.Body.DeserializeJson<PermissionApiModel>();

            post = await _browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = "editpatient" + Guid.NewGuid()
                });
            });

            var editPatientPermission = post.Body.DeserializeJson<PermissionApiModel>();

            // Adding roles
            var role = new RoleApiModel
            {
                Grain = "app",
                SecurableItem = _securableItem,
                Name = "viewer" + Guid.NewGuid(),
                Permissions = new List<PermissionApiModel> { viewPatientPermission }
            };

            post = await _browser.Post("/roles", with => // -3
            {
                with.HttpRequest();
                with.JsonBody(role);
            });

            var viewerRole = post.Body.DeserializeJson<RoleApiModel>();

            post = await _browser.Post("/roles", with => // -2
            {
                with.HttpRequest();
                role.Name = "editor" + Guid.NewGuid();
                role.Permissions = new List<PermissionApiModel> { editPatientPermission };
                with.JsonBody(role);
            });

            var editorRole = post.Body.DeserializeJson<RoleApiModel>();

            // Adding groups
            await _browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Id = Group1,
                    GroupName = Group1,
                    GroupSource = Source1
                });
            });

            await _browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Id = Group2,
                    GroupName = Group2,
                    GroupSource = Source2
                });
            });

            await _browser.Post($"/groups/{Group1}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new
                    {
                        viewerRole.Grain,
                        viewerRole.SecurableItem,
                        viewerRole.Name,
                        viewerRole.Id
                    }
                });
            });

            await _browser.Post($"/groups/{Group2}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new
                    {
                        editorRole.Grain,
                        editorRole.SecurableItem,
                        editorRole.Name,
                        editorRole.Id
                    }
                });
            });

            // Adding permission (user also can modify patient, even though role doesn't)
            var modifyPatientPermission = new PermissionApiModel
            {
                Grain = "app",
                SecurableItem = _securableItem,
                Name = "modifypatient" + Guid.NewGuid(),
                PermissionAction = PermissionAction.Allow
            };

            var response = await _browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(modifyPatientPermission);
            });

            modifyPatientPermission.Id = response.Body.DeserializeJson<PermissionApiModel>().Id;

            var subjectId = _securableItem;

            await _browser.Post($"/user/{IdentityProvider}/{subjectId}/permissions", with =>
            {
                with.HttpRequest();
                var perms = new List<PermissionApiModel> { modifyPatientPermission };
                with.JsonBody(perms);
            });

            // Get the permissions
            var get = await _browser.Get("/user/permissions", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            var permissions = get.Body.DeserializeJson<UserPermissionsApiModel>();
            Assert.Contains($"app/{subjectId}.{editPatientPermission.Name}", permissions.Permissions);
            Assert.Contains($"app/{subjectId}.{viewPatientPermission.Name}", permissions.Permissions);
            Assert.Contains($"app/{subjectId}.{modifyPatientPermission.Name}", permissions.Permissions);
            Assert.Equal(4, permissions.Permissions.Count());
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodNameAttribute]
        public async Task Test_GetGroups_UserNotFoundAsync()
        {
            var get = await _browser.Get("/user/foo/bar/groups", with =>
                {
                    with.HttpRequest();
                });

            Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
            Assert.Contains("User with SubjectId: bar and Identity Provider: foo was not found", get.Body.AsString());
        }

        [Fact]
        public async Task TestGetPermissions_FromCustomGroup_SuccessAsync()
        {
            var groupName = "group1" + Guid.NewGuid();

            var groupPostResponse = await _browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = groupName,
                    GroupSource = "Custom"
                });
            });

            Assert.Equal(HttpStatusCode.Created, groupPostResponse.StatusCode);
            var group = JsonConvert.DeserializeObject<GroupRoleApiModel>(groupPostResponse.Body.AsString());

            var userName = "user1" + Guid.NewGuid();
            var identityProvider = "TestIdp";

            var userGroupResponse = await _browser.Post($"/groups/{group.GroupName}/users", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new
                    {
                        SubjectId = userName,
                        IdentityProvider = identityProvider
                    }
                });
            });

            Assert.Equal(HttpStatusCode.OK, userGroupResponse.StatusCode);

            var roleName = "role1" + Guid.NewGuid();
            var roleResponse = await _browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = roleName
                });
            });

            Assert.Equal(HttpStatusCode.Created, roleResponse.StatusCode);
            var role = JsonConvert.DeserializeObject<RoleApiModel>(roleResponse.Body.AsString());

            var permissionName = "permission1" + Guid.NewGuid();
            var permissionResponse = await _browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = _securableItem,
                    Name = permissionName
                });
            });

            Assert.Equal(HttpStatusCode.Created, permissionResponse.StatusCode);
            var permission = JsonConvert.DeserializeObject<PermissionApiModel>(permissionResponse.Body.AsString());

            var groupRoleResponse = await _browser.Post($"/groups/{group.GroupName}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new
                    {
                        role.Grain,
                        role.SecurableItem,
                        role.Name,
                        role.Id
                    }
                });
            });

            Assert.Equal(HttpStatusCode.OK, groupRoleResponse.StatusCode);

            var permissionRoleResponse = await _browser.Post($"/roles/{role.Id}/permissions", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new
                    {
                        permission.Id
                    }
                });
            });

            Assert.Equal(HttpStatusCode.Created, permissionRoleResponse.StatusCode);

            var permissionsResponse = await _browser.Get($"/user/{identityProvider}/{userName}/permissions", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.OK, permissionsResponse.StatusCode);
            var permissions = JsonConvert.DeserializeObject<List<ResolvedPermissionApiModel>>(permissionsResponse.Body.AsString());
            Assert.Single(permissions);

            var resolvedPermission = permissions.First();
            Assert.Equal(permission.Id, resolvedPermission.Id);
            var resolvedRole = resolvedPermission.Roles.Single();
            Assert.Equal(role.Id, resolvedRole.Id);
        }

        private async Task<RoleApiModel> AddUserAndRoleAsync(string identityProvider, string subjectId)
        {
            var response = await _browser.Post("/user", with =>
            {
                with.JsonBody(new
                {
                    identityProvider,
                    subjectId
                });
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var addRoleResponse = await _browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = Domain.Defaults.Authorization.AppGrain,
                    SecurableItem = _securableItem,
                    Name = "TestRole" + Guid.NewGuid()
                });
            });

            Assert.Equal(HttpStatusCode.Created, addRoleResponse.StatusCode);
            var role = addRoleResponse.Body.DeserializeJson<RoleApiModel>();
            return role;
        }
    }
}