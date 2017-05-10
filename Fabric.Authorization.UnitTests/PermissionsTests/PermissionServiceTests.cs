using System;
using System.Collections.Generic;
using Fabric.Authorization.Domain;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Domain.Validators;
using Fabric.Authorization.UnitTests.Mocks;
using Moq;
using Xunit;

namespace Fabric.Authorization.UnitTests.PermissionsTests
{
    public class PermissionServiceTests
    {
        [Fact]
        public void PermissionService_AddPermission_Successful()
        {
            var mockPermissionStore = new Mock<IPermissionStore>()
                .SetupAddPermissions()
                .Create();

            var permissionService = new PermissionService(mockPermissionStore, new PermissionValidator(mockPermissionStore));
            var validationResult = permissionService.ValidatePermission("app", "patientsafety", "manageusers");
            Assert.True(validationResult.ValidationResult.IsValid);
            var permission = permissionService.AddPermission("app", "patientsafety", "manageusers");

            Assert.NotNull(permission);
            Assert.NotNull(permission.Id);

        }
        
        [Theory, MemberData(nameof(RequestData))]
        public void PermissionService_AddPermission_ReturnsInvalidIfModelNotValid(string grain, string resource, string permissionName, int errorCount)
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

            var permissionService = new PermissionService(mockPermissionStore, new PermissionValidator(mockPermissionStore));
            var result = permissionService.ValidatePermission(grain, resource, permissionName);

            Assert.False(result.ValidationResult.IsValid);
            Assert.NotNull(result.ValidationResult.Errors);
            Assert.Equal(errorCount, result.ValidationResult.Errors.Count);

        }

        [Fact]
        public void PermissionService_DeletePermission_Successful()
        {
            var existingPermission = new Permission
            {
                Id = Guid.NewGuid(),
                Grain = "app",
                Resource = "patientsafety",
                Name = "manageusers"
            };
            var mockPermissionStore = new Mock<IPermissionStore>()
                .SetupGetPermissions(new List<Permission>{existingPermission});

            var permissionService = new PermissionService(mockPermissionStore.Object, new PermissionValidator(mockPermissionStore.Object));
            permissionService.DeletePermission(existingPermission);

            mockPermissionStore.Verify();
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
