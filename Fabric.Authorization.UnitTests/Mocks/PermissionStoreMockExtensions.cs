using System;
using System.Collections.Generic;
using System.Linq;
using Fabric.Authorization.Domain;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;
using Moq;

namespace Fabric.Authorization.UnitTests.Mocks
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
                .Returns((string grain, string securableItem, string permissionName) => 
                    permissions.Where(p => 
                        p.Grain == grain 
                        && p.SecurableItem == securableItem 
                        && (p.Name == permissionName || string.IsNullOrEmpty(permissionName))));

            
            return mockPermissionStore.SetupGetPermission(permissions);
        }

        private static Mock<IPermissionStore> SetupGetPermission(this Mock<IPermissionStore> mockPermissionStore,
            List<Permission> permissions)
        {
            mockPermissionStore.Setup(permissionStore => permissionStore.GetPermission(It.IsAny<Guid>()))
                .Returns((Guid permissionId) => {
                    if (permissions.Any(p => p.Id == permissionId))
                    {
                        return permissions.First(p => p.Id == permissionId);
                    };
                    throw new PermissionNotFoundException();
                });
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
