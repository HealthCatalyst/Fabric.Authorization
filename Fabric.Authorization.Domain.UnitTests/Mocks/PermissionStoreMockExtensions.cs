using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fabric.Authorization.Domain.Permissions;
using Moq;

namespace Fabric.Authorization.Domain.UnitTests.Mocks
{
    public static class PermissionStoreMockExtensions
    {

        public static Mock<IPermissionStore> SetupAddPermissions(this Mock<IPermissionStore> mockPermissionStore)
        {
            mockPermissionStore.Setup(permissionStore => permissionStore.AddPermission(It.IsAny<Permission>()))
                .Returns((Permission p) =>
                {
                    p.Id = Guid.NewGuid();
                    return p;
                });
            return mockPermissionStore;
        }

        public static Mock<IPermissionStore> SetupGetPermissions(this Mock<IPermissionStore> mockPermissionStore, List<Permission> permissions)
        {
            mockPermissionStore
                .Setup(permissionStore => permissionStore.GetPermissions(It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns((string grain, string resource, string permissionName) => permissions.Where(p => p.Grain == grain && p.Resource == resource && p.Name == permissionName));
            return mockPermissionStore;
        }

        public static Mock<IPermissionStore> SetupGetPermission(this Mock<IPermissionStore> mockPermissionStore,
            Permission permission)
        {
            mockPermissionStore.Setup(permissionStore => permissionStore.GetPermission(It.IsAny<Guid>()))
                .Returns(permission);
            return mockPermissionStore;
        }

        public static Mock<IPermissionStore> SetupDeletePermission(this Mock<IPermissionStore> mockPermissionStore)
        {
            mockPermissionStore.Setup(permissionStore => permissionStore.DeletePermission(It.IsAny<Permission>())).Verifiable();
            return mockPermissionStore;
        }

        public static IPermissionStore Create(this Mock<IPermissionStore> mockPermissionStore)
        {
            return mockPermissionStore.Object;
        }
    }
}
