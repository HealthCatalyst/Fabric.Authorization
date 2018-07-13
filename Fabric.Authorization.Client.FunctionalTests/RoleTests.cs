using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Fabric.Authorization.Models;
using Xunit;

namespace Fabric.Authorization.Client.FunctionalTests
{
    [Collection(FunctionalTestConstants.FunctionTestTitle)]
    public class RoleTests : BaseTest
    {
        public RoleTests(FunctionalTestFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public async void AddAndGetRole_ValidRequest_Success()
        {
            var accessToken = await fixture.GetAccessTokenForAuthClient();

            var roleName = Guid.NewGuid().ToString();
            var role = await _authorizationClient.AddRole(accessToken, new RoleApiModel
            {
                Grain = "app",
                SecurableItem = FunctionalTestConstants.IdentityTestUser,
                Name = roleName
            });

            Assert.NotNull(role);

            // get by grain + securableItem
            var roles = await _authorizationClient.GetRole(accessToken, "app", FunctionalTestConstants.IdentityTestUser);
            Assert.NotNull(roles);
            Assert.Contains(roles, r => r.Name == roleName);
        }

        [Fact]
        public async void AddRole_InvalidRequest_Exception()
        {
            try
            {
                var accessToken = await fixture.GetAccessTokenForAuthClient();
                await _authorizationClient.AddRole(accessToken, new RoleApiModel());
            }
            catch (AuthorizationException e)
            {
                Assert.Equal(e.Details.Code, HttpStatusCode.BadRequest.ToString());
                Assert.Contains("Please specify a Grain for this role", e.Details.Details.Select(d => d.Message));
            }
        }

        [Fact]
        public async void AddAndRemoveRolePermission_ValidRequest_Success()
        {
            var accessToken = await fixture.GetAccessTokenForAuthClient();

            // create role
            var roleName = Guid.NewGuid().ToString();
            var role = await _authorizationClient.AddRole(accessToken, new RoleApiModel
            {
                Grain = "app",
                SecurableItem = FunctionalTestConstants.IdentityTestUser,
                Name = roleName
            });

            Assert.NotNull(role);

            // create permission
            var permissionName = Guid.NewGuid().ToString();
            var permission = await _authorizationClient.AddPermission(accessToken, new PermissionApiModel
            {
                Grain = "app",
                SecurableItem = FunctionalTestConstants.IdentityTestUser,
                Name = permissionName
            });

            Assert.NotNull(permission);

            // add permission to role
            role = await _authorizationClient.AddPermissionToRole(accessToken, role.Id.ToString(), new List<PermissionApiModel>
            {
                permission
            });

            Assert.Contains(role.Permissions, p => p.Name == permission.Name);

            // remove permission from role
            role = await _authorizationClient.DeletePermissionsFromRole(accessToken, role.Id.ToString(), new List<PermissionApiModel>
            {
                permission
            });

            Assert.Empty(role.Permissions);
        }
    }
}