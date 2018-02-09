using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.UnitTests.Mocks;
using Moq;
using Xunit;

namespace Fabric.Authorization.UnitTests.Permissions
{
    public class PermissionServiceTests
    {
        [Fact]
        public async Task PermissionService_AddPermission_SuccessfulAsync()
        {
            var mockPermissionStore = new Mock<IPermissionStore>()
                .SetupAddPermissions(new List<Permission>())
                .Create();

            var mockRoleStore = new Mock<IRoleStore>().Object;

            var permissionService = new PermissionService(
                mockPermissionStore,
                new Mock<RoleService>(mockRoleStore, mockPermissionStore).Object);

            var permission = await permissionService.AddPermission(new Permission
            {
                Grain = "app",
                SecurableItem = "patientsafety",
                Name = "manageusers"
            });

            Assert.NotNull(permission);
        }

        [Fact]
        public async Task PermissionService_DeletePermission_SuccessfulAsync()
        {
            var existingPermission = new Permission
            {
                Id = Guid.NewGuid(),
                Grain = "app",
                SecurableItem = "patientsafety",
                Name = "manageusers"
            };
            var mockPermissionStore = new Mock<IPermissionStore>()
                .SetupGetPermissions(new List<Permission> { existingPermission });

            var mockRoleStore = new Mock<IRoleStore>().Object;

            var permissionService = new PermissionService(
                mockPermissionStore.Object,
                new Mock<RoleService>(mockRoleStore, mockPermissionStore.Object).Object);

            await permissionService.DeletePermission(existingPermission);

            mockPermissionStore.Verify();
        }
    }
}