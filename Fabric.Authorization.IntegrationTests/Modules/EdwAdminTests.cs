using Catalyst.Fabric.Authorization.Models;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.Persistence.SqlServer.Configuration;
using Nancy;
using Nancy.Testing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using System.Linq;
using Fabric.Authorization.Persistence.SqlServer.Stores.EDW;

namespace Fabric.Authorization.IntegrationTests.Modules
{
    public class EdwAdminTests : IClassFixture<IntegrationTestsFixture>
    {
        private readonly string _adminRole = "jobadmin";
        private readonly string _dosAdminsGroup = "DosAdmins";
        private readonly string _edwAdminRole = "EDW Admin";
        private readonly string _clientId = "fabric-installer";
        private readonly string _grain = "dos";
        private readonly string _identityProvider = "windows";

        private readonly ISecurityContext _securityContext;
        private readonly Browser _browser;

        public EdwAdminTests(IntegrationTestsFixture fixture, string storageProvider = StorageProviders.InMemory, ConnectionStrings connectionStrings = null)
        {
            if (connectionStrings != null)
            {
                fixture.ConnectionStrings = connectionStrings;
            }

            _securityContext = fixture.GetEdwAdminContext(storageProvider);
            CreateEDWAdminRole(_securityContext, _edwAdminRole, "Edw admin role in the database");

            var principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(Claims.Scope, Scopes.ManageClientsScope),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.Scope, Scopes.InternalScope),
                new Claim(Claims.Scope, Scopes.ManageDosScope),
                new Claim(Claims.ClientId, _clientId)
            }, "testprincipal"));

            _browser = fixture.GetBrowser(principal, storageProvider);
            fixture.CreateClient(_browser, _clientId);
        }

        [Fact]
        public async Task SyncPermissions_NotFoundAsync()
        {
            // Arrange
            var group = new GroupUserApiModel { GroupName = "group" };

            // Act 
            var result = await _browser.Post("/edw/group/roles", with => with.JsonBody(group));

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        }

        [Fact]
        public async Task SyncPermissions_OnRole_AddRemoveToEdwAdminAsync()
        {
            // Arrange I Add user to role
            var role = await CreateRoleAsync(_adminRole);
            var user = await CreateUserAsync("User-" + Guid.NewGuid(), _identityProvider);
            await AddUserToIdentityBASEAsync(_securityContext, user);
            await AssociateUserToRoleAsync(user, role);
            var body = JsonConvert.SerializeObject(new[] { new { identityProvider = user.IdentityProvider, subjectId = user.SubjectId } });

            // Act I Add user to role
            var result = await _browser.Post($"/edw/roles", with => with.Body(body));

            // Assert I Add user to role
            Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
            await AssertEdwAdminRoleOnUserAsync(user, false);

            // Arrange II Remove user from role
            await RemoveUserFromRoleAsync(user, role);

            // Act II Remove user from role
            var result2 = await _browser.Post($"/edw/roles", with => with.Body(body));

            // Assert II Remove user from role
            Assert.Equal(HttpStatusCode.NoContent, result2.StatusCode);
            await AssertEdwAdminRoleOnUserAsync(user, false);
        }

        [Fact]
        public async Task SyncPermissions_OnRole_DoesNotSyncNonWindowsAdUserAsync()
        {
            // Arrange give user admin role
            var role = await CreateRoleAsync(_adminRole);
            var user = await CreateUserAsync("User-" + Guid.NewGuid(), "notwindows");
            await AddUserToIdentityBASEAsync(_securityContext, user);
            await AssociateUserToRoleAsync(user, role);
            var body = JsonConvert.SerializeObject(new[] { new { identityProvider = user.IdentityProvider, subjectId = user.SubjectId } });

            // Act Attempt to sync permissions
            var result = await _browser.Post($"/edw/roles", with => with.Body(body));

            // Assert User not an EDWAdmin
            await AssertEdwAdminRoleOnUserAsync(user, false);
        }

        [Fact]
        public async Task SyncPermissions_OnGroup_DoesNotSyncNonWindowsAdUserAsync()
        {
            // Arrange Add admin role, and user to admin group
            var role = await CreateRoleAsync(_adminRole);
            var group = await CreateGroupAsync("dosadminsgroup" + Guid.NewGuid());
            await AssociateGroupToRoleAsync(group, role);
            var user = await CreateUserAsync("User-" + Guid.NewGuid(), "nonwindows");
            await AddUserToIdentityBASEAsync(_securityContext, user);
            await AssociateUserToGroupAsync(user, group);

            // Act Attempt to sync permissions
            var result = await _browser.Post($"/edw/{group.GroupName}/roles", with => with.Body(""));

            // Assert
            await AssertEdwAdminRoleOnUserAsync(user, false);
        }

        [Fact]
        public async Task SyncPermissions_OnDosAdminsGroup_DoesNotSyncNonWindowsAdUserAsync()
        {
            // Arrange Add DosAdmins group, and user to DosAdmins group
            var group = await CreateGroupAsync(_dosAdminsGroup);
            var user = await CreateUserAsync("User-" + Guid.NewGuid(), "nonwindows");
            await AddUserToIdentityBASEAsync(_securityContext, user);
            await AssociateUserToGroupAsync(user, group);

            // Act Attempt to sync permissions
            var result = await _browser.Post($"/edw/{group.GroupName}/roles", with => with.Body(""));

            // Assert
            await AssertEdwAdminRoleOnUserAsync(user, false);
        }

        [Fact]
        public async Task SyncPermissions_OnGroup_AddRemoveToEdwAdminAsync()
        {
            // Arrange I Add role to group
            var role = await CreateRoleAsync(_adminRole);
            var group = await CreateGroupAsync("dosadminsgroup" + Guid.NewGuid());
            await AssociateGroupToRoleAsync(group, role);
            var user = await CreateUserAsync("User-" + Guid.NewGuid(), _identityProvider);
            await AddUserToIdentityBASEAsync(_securityContext, user);
            await AssociateUserToGroupAsync(user, group);

            // Act I add role to group
            var result = await _browser.Post($"/edw/{group.GroupName}/roles", with => with.Body(""));

            // Assert I add role to group
            Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
            await AssertEdwAdminRoleOnUserAsync(user, false);

            // Arrange II remove role from group
            await RemoveRoleFromGroupAsync(group, role);

            // Act II remove role from group
            var result2 = await _browser.Post($"/edw/{group.GroupName}/roles", with => with.Body(""));

            // Assert II Remove role from group
            Assert.Equal(HttpStatusCode.NoContent, result2.StatusCode);
            await AssertEdwAdminRoleOnUserAsync(user, false);
        }

        [Fact]
        public async Task SyncPermissions_OnGroup_RemoveAddUserFromGroupAsync()
        {
            // Arrange I Add role to group
            var role = await CreateRoleAsync(_adminRole);
            var group = await CreateGroupAsync("dosadminsgroup" + Guid.NewGuid());
            await AssociateGroupToRoleAsync(group, role);
            var user = await CreateUserAsync("User-" + Guid.NewGuid(), _identityProvider);
            await AddUserToIdentityBASEAsync(_securityContext, user);
            await AssociateUserToGroupAsync(user, group);

            // Act I add role to group
            var result = await _browser.Post($"/edw/{group.GroupName}/roles", with => with.Body(""));

            // Assert I add role to group
            Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
            await AssertEdwAdminRoleOnUserAsync(user, false);

            // Arrange II remove role from group
            await RemoveUserFromGroupAsync(group, user);
            var body = JsonConvert.SerializeObject(new[] { new { identityProvider = user.IdentityProvider, subjectId = user.SubjectId } });

            // Act II remove role from group
            var result2 = await _browser.Post($"/edw/roles", with => with.Body(body));

            // Assert II Remove role from group
            Assert.Equal(HttpStatusCode.NoContent, result2.StatusCode);
            await AssertEdwAdminRoleOnUserAsync(user, false);
        }

        [Fact]
        public async Task SyncPermissions_OnGroup_UserStillHasRoleAsync()
        {
            // Arrange I Add role to group
            var role = await CreateRoleAsync(_adminRole);
            var group = await CreateGroupAsync("dosadminsgroup" + Guid.NewGuid());
            await AssociateGroupToRoleAsync(group, role);
            var user = await CreateUserAsync("User-" + Guid.NewGuid(), _identityProvider);
            await AddUserToIdentityBASEAsync(_securityContext, user);
            await AssociateUserToGroupAsync(user, group);
            await AssociateUserToRoleAsync(user, role);

            // Act I add role to group
            var result = await _browser.Post($"/edw/{group.GroupName}/roles");

            // Assert I add role to group
            Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
            await AssertEdwAdminRoleOnUserAsync(user, false);

            // Arrange II remove role from group
            await RemoveUserFromGroupAsync(group, user);
            var body = JsonConvert.SerializeObject(new[] { new { identityProvider = user.IdentityProvider, subjectId = user.SubjectId } });

            // Act II remove role from group
            var result2 = await _browser.Post($"/edw/roles", with => with.Body(body));

            // Assert II Remove role from group
            Assert.Equal(HttpStatusCode.NoContent, result2.StatusCode);
            await AssertEdwAdminRoleOnUserAsync(user, false);
        }

        private Task AddUserToIdentityBASEAsync(ISecurityContext securityContext, UserApiModel user)
        {
            return Task.Run(() =>
            {
                if (!securityContext.EDWIdentities.Where(p => p.Name == user.SubjectId).Any())
                {
                    securityContext.EDWIdentities.Add(new Persistence.SqlServer.EntityModels.EDW.EDWIdentity()
                    {
                        Name = user.SubjectId
                    });

                    securityContext.SaveChanges();
                }
            });
        }

        private void CreateEDWAdminRole(ISecurityContext securityContext, string roleName, string description)
        {
            if (!securityContext.EDWRoles.Where(p => p.Name == roleName).Any())
            {
                securityContext.EDWRoles.Add(new Persistence.SqlServer.EntityModels.EDW.EDWRole()
                {
                    Name = roleName,
                    Description = description
                });

                securityContext.SaveChanges();
            }
        }

        private async Task RemoveUserFromGroupAsync(GroupRoleApiModel group, UserApiModel user)
        {
            var groupRoleResponse = await _browser.Delete($"/groups/{group.GroupName}/users", with =>
            {
                with.HttpRequest();
                with.JsonBody(
                    new { GroupName = group.Id, SubjectId = user.SubjectId, IdentityProvider = user.IdentityProvider }
                );
            });
            Assert.Equal(HttpStatusCode.OK, groupRoleResponse.StatusCode);
        }

        private async Task RemoveRoleFromGroupAsync(GroupRoleApiModel group, RoleApiModel role)
        {
            var groupRoleResponse = await _browser.Delete($"/groups/{group.GroupName}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[] {
                    new { RoleId = role.Id }
                });
            });
            Assert.Equal(HttpStatusCode.OK, groupRoleResponse.StatusCode);
        }

        private async Task RemoveUserFromRoleAsync(UserApiModel user, RoleApiModel role)
        {
            var userRole = await _browser.Delete($"/user/{user.IdentityProvider}/{user.SubjectId}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[] {
                    role
                });
            });
            Assert.Equal(HttpStatusCode.OK, userRole.StatusCode);
        }

        private Task AssertEdwAdminRoleOnUserAsync(UserApiModel user, bool isAdded)
        {
            return Task.Run(() =>
            {
                var role = _securityContext.EDWRoles.Where(p => p.Name == _edwAdminRole).FirstOrDefault();
                var identity = _securityContext.EDWIdentities.Where(p => p.Name == user.SubjectId).FirstOrDefault();

                var hasUser = _securityContext.EDWIdentityRoles.Any(p => p.RoleID == role.Id && p.IdentityID == identity.Id);

                if (isAdded)
                {
                    Assert.True(hasUser);
                }
                else
                {
                    Assert.False(hasUser);
                }
            });
        }

        private async Task AssociateUserToRoleAsync(UserApiModel user, RoleApiModel role)
        {
            var groupRoleResponse = await _browser.Post($"/user/{user.IdentityProvider}/{user.SubjectId}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    role
                });
            });
            Assert.Equal(HttpStatusCode.OK, groupRoleResponse.StatusCode);
        }

        private async Task<RoleApiModel> CreateRoleAsync(string name)
        {
            var Grain = _grain;
            var SecurableItem = _clientId;

            var getResponse = await _browser.Get($"/roles/{Grain}/{SecurableItem}/{name}");
            var result = getResponse.Body.AsString();
            if (getResponse.StatusCode == HttpStatusCode.OK && result != "[]")
            {
                return JsonConvert.DeserializeObject<RoleApiModel[]>(result).FirstOrDefault();
            }
            else if (result == "[]" || getResponse.StatusCode == HttpStatusCode.NotFound)
            {

                var roleResponse = await _browser.Post("/roles", with =>
                {
                    with.HttpRequest();
                    with.JsonBody(new
                    {
                        Grain = "dos",
                        SecurableItem = _clientId,
                        Name = name,
                        DisplayName = "dosadmindisplay",
                        Description = "dosadmindescription"
                    });
                });
                Assert.Equal(HttpStatusCode.Created, roleResponse.StatusCode);
                return JsonConvert.DeserializeObject<RoleApiModel>(roleResponse.Body.AsString());
            }
            else
            {
                throw new NotImplementedException("Could not create Role.");
            }
        }

        private async Task<GroupRoleApiModel> CreateGroupAsync(string name)
        {
            var groupResponse = await _browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = name,
                    GroupSource = "Custom"
                });
            });
            Assert.Equal(HttpStatusCode.Created, groupResponse.StatusCode);
            var group = JsonConvert.DeserializeObject<GroupRoleApiModel>(groupResponse.Body.AsString());
            return group;
        }

        private async Task AssociateGroupToRoleAsync(GroupRoleApiModel group, RoleApiModel role)
        {
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
        }

        private async Task<UserApiModel> CreateUserAsync(string subjectId, string identityProvider)
        {
            var userResponse = await _browser.Post("/user", with =>
            {
                with.JsonBody(new
                {
                    identityProvider = identityProvider,
                    subjectId = subjectId
                });
            });
            Assert.Equal(HttpStatusCode.Created, userResponse.StatusCode);
            var user = JsonConvert.DeserializeObject<UserApiModel>(userResponse.Body.AsString());
            return user;
        }

        private async Task AssociateUserToGroupAsync(UserApiModel user, GroupRoleApiModel group)
        {
            var groupUserResponse = await _browser.Post($"/groups/{group.GroupName}/users", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.JsonBody(new[]
                {
                    new
                    {
                         user.SubjectId,
                         user.IdentityProvider
                    }
                });
            });
            Assert.Equal(HttpStatusCode.OK, groupUserResponse.StatusCode);
        }
    }
}
