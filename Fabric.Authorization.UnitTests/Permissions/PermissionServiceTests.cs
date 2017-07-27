using System;
using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Domain.Stores.Services;
using Fabric.Authorization.UnitTests.Mocks;
using Moq;
using Xunit;

namespace Fabric.Authorization.UnitTests.Permissions
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
            var permission = permissionService.AddPermission(new Permission
            {
                Grain = "app",
                SecurableItem = "patientsafety",
                Name = "manageusers"
            });

            Assert.NotNull(permission);
            Assert.NotNull(permission.Id);

        }

        [Fact]
        public void PermissionService_DeletePermission_Successful()
        {
            var existingPermission = new Permission
            {
                Id = Guid.NewGuid(),
                Grain = "app",
                SecurableItem = "patientsafety",
                Name = "manageusers"
            };
            var mockPermissionStore = new Mock<IPermissionStore>()
                .SetupGetPermissions(new List<Permission>{existingPermission});

            var permissionService = new PermissionService(mockPermissionStore.Object);
            permissionService.DeletePermission(existingPermission);

            mockPermissionStore.Verify();
        }

    }
}
