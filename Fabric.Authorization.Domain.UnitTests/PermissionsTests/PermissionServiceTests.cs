using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Permissions;
using Fabric.Authorization.Domain.UnitTests.Mocks;
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

            var permissionService = new PermissionService(mockPermissionStore);
            var permission = permissionService.AddPermission("app", "patientsafety", "manageusers");

            Assert.NotNull(permission);
            Assert.NotNull(permission.Id);

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

            var permissionService = new PermissionService(mockPermissionStore);

            Assert.Throws<PermissionAlreadyExistsException>(
                () => permissionService.AddPermission(existingPermission.Grain, existingPermission.Resource, existingPermission.Name));

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

            var permissionService = new PermissionService(mockPermissionStore.Object);
            permissionService.DeletePermission(existingPermission.Id);

            mockPermissionStore.Verify();
        }
    }
}
