﻿using Catalyst.Fabric.Authorization.Models;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.Persistence.SqlServer.Configuration;
using Nancy;
using Nancy.Testing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using System.Linq;
using Fabric.Authorization.Persistence.SqlServer.EntityModels;
using Fabric.Authorization.Persistence.SqlServer.Stores.EDW;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Persistence.SqlServer.Stores;

namespace Fabric.Authorization.IntegrationTests.Modules
{
    public class EdwAdminTests : IClassFixture<IntegrationTestsFixture>
    {
        private readonly ConnectionStrings _connectionStrings;
        private readonly ISecurityContext _securityContext;
        private readonly string _adminRole = "jobsadmin";
        private readonly Browser _browser;
        private readonly IntegrationTestsFixture _fixture;
        private readonly string _storageProvider;
        private readonly string _clientId;

        public EdwAdminTests(IntegrationTestsFixture fixture, string storageProvider = StorageProviders.InMemory, ConnectionStrings connectionStrings = null)
        {
            if (connectionStrings != null)
            {
                fixture.ConnectionStrings = connectionStrings;
                _connectionStrings = connectionStrings;
                _securityContext = new SecurityContext(_connectionStrings);
            }

            _clientId = "fabric-installer";

            var principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(Claims.Scope, Scopes.ManageClientsScope),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.Scope, Scopes.ManageDosScope),
                new Claim(Claims.ClientId, _clientId)
            }, "testprincipal"));

            _storageProvider = storageProvider;
            _fixture = fixture;
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
            var role = await CreateRoleAsync();
            var user = await CreateUserAsync();
            await AssociateUserToRoleAsync(user, role);

            // Act I Add user to role
            var result = await _browser.Post($"/edw/{user.SubjectId}/{user.IdentityProvider}/roles", with => with.Body(""));

            // Assert I Add user to role
            Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
            AssertEdwAdminRoleOnUserAsync(user, true);

            // Arrange II Remove user from role
            await RemoveUserFromRoleAsync(user, role);

            // Act II Remove user from role
            var result2 = await _browser.Post($"/edw/{user.SubjectId}/{user.IdentityProvider}/roles", with => with.Body(""));

            // Assert II Remove user from role
            Assert.Equal(HttpStatusCode.NoContent, result2.StatusCode);
            AssertEdwAdminRoleOnUserAsync(user, false);
        }
        
        [Fact]
        public async Task SyncPermissions_OnGroup_AddRemoveToEdwAdminAsync()
        {
            // Arrange I Add role to group
            var role = await CreateRoleAsync();
            var group = await CreateGroupAsync();
            await AssociateGroupToRoleAsync(group, role);
            var user = await CreateUserAsync();
            await AssociateUserToGroupAsync(user, group);

            // Act I add role to group
            var result = await _browser.Post($"/edw/{group.GroupName}/roles", with => with.Body(""));

            // Assert I add role to group
            Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
            AssertEdwAdminRoleOnUserAsync(user, true);

            // Arrange II remove role from group
            await RemoveRoleFromGroupAsync(group, role);

            // Act II remove role from group
            var result2 = await _browser.Post($"/edw/{group.GroupName}/roles", with => with.Body(""));

            // Assert II Remove role from group
            Assert.Equal(HttpStatusCode.NoContent, result2.StatusCode);
            AssertEdwAdminRoleOnUserAsync(user, false);
        }

        [Fact]
        public async Task SyncPermissions_OnGroup_RemoveAddUserFromGroupAsync()
        {
            // Arrange I Add role to group
            var role = await CreateRoleAsync();
            var group = await CreateGroupAsync();
            await AssociateGroupToRoleAsync(group, role);
            var user = await CreateUserAsync();
            await AssociateUserToGroupAsync(user, group);

            // Act I add role to group
            var result = await _browser.Post($"/edw/{group.GroupName}/roles", with => with.Body(""));

            // Assert I add role to group
            Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
            AssertEdwAdminRoleOnUserAsync(user, true);

            // Arrange II remove role from group
            await RemoveUserFromGroupAsync(group, user);

            // Act II remove role from group
            var result2 = await _browser.Post($"/edw/{user.SubjectId}/{user.IdentityProvider}/roles", with => with.Body(""));

            // Assert II Remove role from group
            Assert.Equal(HttpStatusCode.NoContent, result2.StatusCode);
            AssertEdwAdminRoleOnUserAsync(user, false);
        }

        [Fact]
        public async Task SyncPermissions_OnGroup_UserStillHasRoleAsync()
        {
            // Arrange I Add role to group
            var role = await CreateRoleAsync();
            var group = await CreateGroupAsync();
            await AssociateGroupToRoleAsync(group, role);
            var user = await CreateUserAsync();
            await AssociateUserToGroupAsync(user, group);
            await AssociateUserToRoleAsync(user, role);

            // Act I add role to group
            var result = await _browser.Post($"/edw/{group.GroupName}/roles", with => with.Body(""));

            // Assert I add role to group
            Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
            AssertEdwAdminRoleOnUserAsync(user, true);

            // Arrange II remove role from group
            await RemoveUserFromGroupAsync(group, user);

            // Act II remove role from group
            var result2 = await _browser.Post($"/edw/{user.SubjectId}/{user.IdentityProvider}/roles", with => with.Body(""));

            // Assert II Remove role from group
            Assert.Equal(HttpStatusCode.NoContent, result2.StatusCode);
            AssertEdwAdminRoleOnUserAsync(user, true);
        }

        private async Task RemoveUserFromGroupAsync(GroupRoleApiModel group, UserApiModel user)
        {
            var groupRoleResponse = await _browser.Delete($"/group/{group.GroupName}/users", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[] {
                    new { GroupName = group.Id, SubjectId = user.SubjectId, IdentityProvider = user.IdentityProvider }
                });
            });
            Assert.Equal(HttpStatusCode.OK, groupRoleResponse.StatusCode);
        }
        
        private async Task RemoveRoleFromGroupAsync(GroupRoleApiModel group, RoleApiModel role)
        {
            var groupRoleResponse = await _browser.Delete($"/group/{group.GroupName}/roles", with =>
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
            return Task.Run(() => {
                var role = _securityContext.EDWRoles.Where(p => p.Name == _adminRole).FirstOrDefault();
                var identity = _securityContext.EDWIdentities.Where(p => p.Name == user.SubjectId).FirstOrDefault();

                var hasUser = _securityContext.EDWIdentityRoles.Any(p => p.RoleID == role.Id && p.IdentityID == identity.Id);

                if(isAdded)
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

        private async Task<RoleApiModel> CreateRoleAsync()
        {
            var roleResponse = await _browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "dos",
                    SecurableItem = _clientId,
                    Name = _adminRole,
                    DisplayName = "dosadmindisplay",
                    Description = "dosadmindescription"
                });
            });
            Assert.Equal(HttpStatusCode.Created, roleResponse.StatusCode);
            var role = JsonConvert.DeserializeObject<RoleApiModel>(roleResponse.Body.AsString());
            return role;
        }

        private async Task<GroupRoleApiModel> CreateGroupAsync()
        {
            var groupResponse = await _browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = "dosadmins" + Guid.NewGuid(),
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

        private async Task<UserApiModel> CreateUserAsync()
        {
            var userResponse = await _browser.Post("/user", with =>
            {
                with.JsonBody(new
                {
                    identityProvider = "temp",
                    subjectId = "tempUser"
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
