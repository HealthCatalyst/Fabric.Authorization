namespace Fabric.Authorization.Client.FunctionalTests
{
    using Models;
    using Models.Enums;
    using System.Linq;
    using System;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(FunctionalTestConstants.FunctionTestTitle)]
    public class UserPermissionTests : BaseTest
    {
        public UserPermissionTests(FunctionalTestFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public async Task GetPermissionsForCurrentUser_InvalidToken_ThrowException()
        {
            // arrange
            string token = "invalid";
            try
            { // act
                await this._authorizationClient.GetPermissionsForCurrentUser(token);
                Assert.False(true, "should not be able to call this method with invalid token");
            }
            catch (Exception ex)
            {
                // assert
                AuthorizationException authException = ex as AuthorizationException;
                Assert.NotNull(authException);
                Assert.Contains(HttpStatusCode.Forbidden.ToString(), authException.Details.Code);
            }
        }

        [Fact]
        public async Task GetPermissionsForCurrentUser_NullToken_ThrowException()
        {
            try
            {
                // act
                await this._authorizationClient.GetPermissionsForCurrentUser(null);
                Assert.False(true, "should not be able to call this method with invalid token");
            }
            catch (Exception ex)
            {
                // assert
                AuthorizationException authException = ex as AuthorizationException;
                Assert.NotNull(authException);
                Assert.Contains("cannot be null or empty", authException.Message);
            }
        }

        [Fact]
        public async Task GetPermissionsForCurrentUser_GetValidUser_OK()
        {
            // arrange
            string token = await this.fixture.GetUserAccessToken(FunctionalTestConstants.BobUserName, FunctionalTestConstants.BobPassword);

            // act
            var result = await this._authorizationClient.GetPermissionsForCurrentUser(token);

            // assert
            Assert.NotNull(result);
            Assert.True(result.Permissions.Any<string>());
            Assert.True(result.PermissionRequestContexts.Any<PermissionRequestContext>());
        }

        [Fact]
        public async Task GetPermissionsForCurrentUser_GrainAndSecurableItems_GetValidUser_OK()
        {
            // arrange
            string token = await this.fixture.GetUserAccessToken(FunctionalTestConstants.BobUserName, FunctionalTestConstants.BobPassword);

            // act
            var result = await this._authorizationClient.GetPermissionsForCurrentUser(token, FunctionalTestConstants.Grain, FunctionalTestConstants.IdentityTestUser);

            // assert
            Assert.NotNull(result);
            Assert.True(result.Permissions.Any<string>());
            Assert.True(result.PermissionRequestContexts.Any<PermissionRequestContext>());
        }

        [Fact]
        public async Task GetPermissionsForCurrentUser_InvalidGrain_RetrievesNoPermissions()
        {
            // arrange
            string token = await this.fixture.GetUserAccessToken(FunctionalTestConstants.BobUserName, FunctionalTestConstants.BobPassword);

            // act
            var result = await this._authorizationClient.GetPermissionsForCurrentUser(token, "EmptyApp", "EmptyApp");

            // assert
            Assert.NotNull(result);
            Assert.False(result.Permissions.Any<string>());
            Assert.False(result.PermissionRequestContexts.Any<PermissionRequestContext>());
        }

        [Fact]
        public async Task DoesUserHavePermission_UserCanView_True()
        {
            // arrange
            string token = await this.fixture.GetUserAccessToken(FunctionalTestConstants.BobUserName, FunctionalTestConstants.BobPassword);
            string permissions = $"{FunctionalTestConstants.Grain}/{FunctionalTestConstants.IdentityTestUser}.userCanView";

            // act
            var result = await this._authorizationClient.DoesUserHavePermission(token, permissions);

            // assert
            Assert.True(result);
        }

        [Fact]
        public async Task DoesUserHavePermission_InvalidPermission_False()
        {
            // arrange
            string token = await this.fixture.GetUserAccessToken(FunctionalTestConstants.BobUserName, FunctionalTestConstants.BobPassword);
            var userPermissions = await this._authorizationClient.GetPermissionsForCurrentUser(token);
            string permissions = $"{FunctionalTestConstants.Grain}/{FunctionalTestConstants.IdentityTestUser}.userCanView2";

            // act
            var result = this._authorizationClient.DoesUserHavePermission(userPermissions, permissions);

            // assert
            Assert.False(result);
        }

    }
}
