namespace Catalyst.Fabric.Authorization.Client.UnitTests
{
    using Models;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using Xunit;

    public class AuthorizationClientTests
    {
        private AuthorizationClient _subject;
        private HttpClient _client;
        private UserPermissionsApiModel _userPermission;

        /// <summary>
        /// This is the test initializer.
        /// </summary>
        public AuthorizationClientTests()
        {
            _client = new HttpClient();
            _subject = new AuthorizationClient(_client);
            _userPermission = new UserPermissionsApiModel
            {
                PermissionRequestContexts = new List<PermissionRequestContext>
                 {
                     new PermissionRequestContext
                     {
                          RequestedGrain = "app",
                          RequestedSecurableItem = "unit-test"
                     }
                 },
                Permissions = new List<string>
                {
                    "edit",
                    "view"
                }
            };
        }

        [Fact]
        public void DoesUserHavePermission_NullPermission_ThrowAuthorizationException()
        {
            // Arrange
            string permission = null;
            var userPermissions = new UserPermissionsApiModel();

            // Act
            try
            {
                _subject.DoesUserHavePermission(userPermissions, permission);
            }
            catch (Exception exc)
            {
                // Assert
                var authorizationException = exc as AuthorizationException;
                Assert.NotNull(authorizationException);
                Assert.Contains("Value permission cannot be null or empty.", exc.Message);
            }
        }

        [Fact]
        public void DoesUserHavePermission_NullUserPermissions_False()
        {
            // Arrange
            string permission = "awesomepermission";
            UserPermissionsApiModel userPermissions = null;

            // Act
            var result = _subject.DoesUserHavePermission(userPermissions, permission);

            //Assert
            Assert.False(result);
        }

        [Fact]
        public void DoesUserHavePermission_InvalidUserPermissions_False()
        {
            // Arrange
            string permission = "admin";
            var userPermissions = _userPermission;

            // Act
            var result = _subject.DoesUserHavePermission(userPermissions, permission);

            //Assert
            Assert.False(result);
        }

        [Fact]
        public void DoesUserHavePermission_MatchUserPermissions_True()
        {
            // Arrange
            string permission = "edit";
            var userPermissions = _userPermission;

            // Act
            var result = _subject.DoesUserHavePermission(userPermissions, permission);

            //Assert
            Assert.True(result);
        }
    }
}
