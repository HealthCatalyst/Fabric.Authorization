using System;
using System.Collections.Generic;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Permissions;
using Fabric.Authorization.Domain.UnitTests.Mocks;
using Fabric.Authorization.Domain.Validators;
using Moq;
using Xunit;

namespace Fabric.Authorization.Domain.UnitTests.PermissionsTests
{
    public class PermissionServiceTests
    {
        [Fact]
        public void PermissionService_AddPermission_Successful()
        {
            var mockPermissionStore = new Mock<IPermissionStore>()
                .SetupAddPermissions()
                .Create();

            var permissionService = new PermissionService(mockPermissionStore, new PermissionValidator());
            var result = permissionService.AddPermission<Permission>("app", "patientsafety", "manageusers");

            Assert.NotNull(result.Model);
            Assert.NotNull(result.Model.Id);

        }

        [Fact]
        public void PermissionService_AddPermission_ThrowsIfPermissionAlreadyExists()
        {
            var existingPermission = new Permission
            {
                Id = Guid.NewGuid(),
                Grain = "app",
                Resource = "patientsafety",
                Name = "manageusers"
            };
            var mockPermissionStore = new Mock<IPermissionStore>()
                .SetupGetPermissions(new List<Permission>
                                            {
                                                existingPermission
                                            })
                .Create();

            var permissionService = new PermissionService(mockPermissionStore, new PermissionValidator());

            Assert.Throws<PermissionAlreadyExistsException>(
                () => permissionService.AddPermission<Permission>(existingPermission.Grain, existingPermission.Resource, existingPermission.Name));

        }

        [Theory, MemberData(nameof(RequestData))]
        public void PermissionService_AddPermission_ReturnsInvalidIfModelNotValid(string grain, string resource, string permissionName, int errorCount)
        {
            var mockPermissionStore = new Mock<IPermissionStore>()
                .SetupAddPermissions()
                .Create();

            var permissionService = new PermissionService(mockPermissionStore, new PermissionValidator());
            var result = permissionService.AddPermission<Permission>(grain, resource, permissionName);

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

            var permissionService = new PermissionService(mockPermissionStore.Object, new PermissionValidator());
            permissionService.DeletePermission(existingPermission.Id);

            mockPermissionStore.Verify();
        }

        public static IEnumerable<object[]> RequestData => new[]
        {
            new object[] { "app", "patientsafety", "", 1},
            new object[] {"app", "", "", 2},
            new object[] {"", "", "", 3},
            new object[] {"app", "patientsafety", null, 1},
            new object[] {"app", null, null, 2},
            new object[] {null, null, null, 3}
        };
    }
}
