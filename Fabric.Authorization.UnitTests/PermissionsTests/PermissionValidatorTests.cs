using System;
using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Domain.Validators;
using Fabric.Authorization.UnitTests.Mocks;
using Moq;
using Xunit;

namespace Fabric.Authorization.UnitTests.PermissionsTests
{
    public class PermissionValidatorTests
    {
        [Theory, MemberData(nameof(RequestData))]
        public void PermissionValidator_ValidatePermission_ReturnsInvalidIfModelNotValid(string grain, string resource, string permissionName, int errorCount)
        {
            var existingPermission = new Permission
            {
                Id = Guid.NewGuid(),
                Grain = "app",
                Resource = "patientsafety",
                Name = "manageusers"
            };

            var mockPermissionStore = new Mock<IPermissionStore>()
                .SetupAddPermissions()
                .SetupGetPermissions(new List<Permission>
                {
                    existingPermission
                })
                .Create();
            
            var permissionValidator = new PermissionValidator(mockPermissionStore);
            var validationResult = permissionValidator.Validate(new Permission
            {
                Grain = grain,
                Resource = resource,
                Name = permissionName
            });

            Assert.False(validationResult.IsValid);
            Assert.NotNull(validationResult.Errors);
            Assert.Equal(errorCount, validationResult.Errors.Count);

        }

        [Fact]
        public void PermissionValidator_ValidatePermission_ReturnsValid()
        {
            var mockPermissionStore = new Mock<IPermissionStore>()
                .SetupAddPermissions()
                .SetupGetPermissions(new List<Permission>())
                .Create();

            var permissionValidator = new PermissionValidator(mockPermissionStore);
            var validationResult = permissionValidator.Validate(new Permission
            {
                Grain = "app",
                Resource = "patientsafety",
                Name = "manageusers"
            });

            Assert.True(validationResult.IsValid);
        }

        public static IEnumerable<object[]> RequestData => new[]
        {
            new object[] { "app", "patientsafety", "", 1},
            new object[] {"app", "", "", 2},
            new object[] {"", "", "", 3},
            new object[] {"app", "patientsafety", null, 1},
            new object[] {"app", null, null, 2},
            new object[] {null, null, null, 3},
            new object[] {"app", "patientsafety", "manageusers", 1}
        };
    }
}
