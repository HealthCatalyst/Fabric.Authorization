using System;
using System.Net;
using Fabric.Authorization.Models;
using Xunit;

namespace Fabric.Authorization.Client.FunctionalTests
{
    [Collection(FunctionalTestConstants.FunctionTestTitle)]
    public class PermissionTests : BaseTest
    {
        public PermissionTests(FunctionalTestFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public async void AddAndGetPermission_ValidRequest_Success()
        {
            var accessToken = await fixture.GetAccessTokenForAuthClient();

            var permissionName = Guid.NewGuid().ToString();
            var permission = await _authorizationClient.AddPermission(accessToken, new PermissionApiModel
            {
                Grain = "app",
                SecurableItem = FunctionalTestConstants.IdentityTestUser,
                Name = permissionName
            });

            Assert.NotNull(permission);
            Assert.NotNull(permission.Id);

            // get by ID
            permission = await _authorizationClient.GetPermission(accessToken, permission.Id.ToString());
            Assert.NotNull(permission);
            Assert.Equal(permissionName, permission.Name);

            // get by grain + securableItem
            var permissions = await _authorizationClient.GetPermissions(accessToken, "app", FunctionalTestConstants.IdentityTestUser);
            Assert.NotNull(permissions);
            Assert.Contains(permissions, p => p.Name == permissionName);
        }

        [Fact]
        public async void AddPermission_InvalidRequest_Exception()
        {
            try
            {
                var accessToken = await fixture.GetAccessTokenForAuthClient();
                await _authorizationClient.AddPermission(accessToken, new PermissionApiModel
                {
                    Grain = "app",
                    SecurableItem = FunctionalTestConstants.IdentityTestUser
                });
            }
            catch (AuthorizationException e)
            {
                Assert.Equal(e.Details.Code, HttpStatusCode.BadRequest.ToString());
                Assert.Equal("Please specify a Name for this permission", e.Details.Message);
            }
        }
    }
}