using System;
using System.Collections.Generic;
using Fabric.Authorization.Models;
using Xunit;

namespace Fabric.Authorization.Client.FunctionalTests
{
    [Collection(FunctionalTestConstants.FunctionTestTitle)]
    public class UserTests : BaseTest
    {
        public UserTests(FunctionalTestFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public async void AddUser_ValidRequest_Success()
        {
            var accessToken = await fixture.GetAccessTokenForAuthClient();

            const string identityProvider = "windows";
            var subjectId = Guid.NewGuid().ToString();
            var user = await _authorizationClient.AddUser(accessToken, new UserApiModel
            {
                IdentityProvider = identityProvider,
                SubjectId = subjectId
            });

            Assert.NotNull(user);
            Assert.Equal(identityProvider, user.IdentityProvider);
            Assert.Equal(subjectId, user.SubjectId);
        }

        [Fact]
        public async void AddAndRemoveUserRole_ValidRequest_Success()
        {
            var accessToken = await fixture.GetAccessTokenForAuthClient();

            // create user
            const string identityProvider = "windows";
            var subjectId = Guid.NewGuid().ToString();
            var user = await _authorizationClient.AddUser(accessToken, new UserApiModel
            {
                IdentityProvider = identityProvider,
                SubjectId = subjectId
            });

            Assert.NotNull(user);

            // create role
            var roleName = Guid.NewGuid().ToString();
            var role = await _authorizationClient.AddRole(accessToken, new RoleApiModel
            {
                Grain = "app",
                SecurableItem = FunctionalTestConstants.IdentityTestUser,
                Name = roleName
            });

            Assert.NotNull(role);
            Assert.NotNull(role.Id);

            // add role to user
            user = await _authorizationClient.AddRolesToUser(accessToken, identityProvider, subjectId, new List<RoleApiModel>
            {
                role
            });

            Assert.Contains(user.Roles, r => r.Name == role.Name);

            // remove role from user
            user = await _authorizationClient.DeleteRolesFromUser(accessToken, identityProvider, subjectId, new List<RoleApiModel>
            {
                new RoleApiModel
                {
                    Id = role.Id,
                    Grain = "app",
                    SecurableItem = FunctionalTestConstants.IdentityTestUser
                }
            });

            Assert.Empty(user.Roles);
        }

        [Fact]
        public async void AddAndRemoveUserPermission_ValidRequest_Success()
        {
            var accessToken = await fixture.GetAccessTokenForAuthClient();

            // create user
            const string identityProvider = "windows";
            var subjectId = Guid.NewGuid().ToString();
            var user = await _authorizationClient.AddUser(accessToken, new UserApiModel
            {
                IdentityProvider = identityProvider,
                SubjectId = subjectId
            });

            Assert.NotNull(user);

            // create permission
            var permissionName = Guid.NewGuid().ToString();
            var permission = await _authorizationClient.AddPermission(accessToken, new PermissionApiModel
            {
                Grain = "app",
                SecurableItem = FunctionalTestConstants.IdentityTestUser,
                Name = permissionName
            });

            Assert.NotNull(permission);
            Assert.NotNull(permission.Id);

            // add permission to user (no content returned so just ensure the call succeeds)
            await _authorizationClient.AddPermissionsToUser(accessToken, identityProvider, subjectId, new List<PermissionApiModel>
            {
                permission
            });

            // remove permission from user (no content returned so just ensure the call succeeds)
            await _authorizationClient.DeletePermissionsFromUser(accessToken, identityProvider, subjectId, new List<PermissionApiModel>
            {
                permission
            });
        }
    }
}