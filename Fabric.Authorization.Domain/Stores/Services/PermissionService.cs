using System;
using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;

namespace Fabric.Authorization.Domain.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly IPermissionStore _permissionStore;

        public PermissionService(IPermissionStore permissionStore)
        {
            _permissionStore = permissionStore ?? throw new ArgumentNullException(nameof(permissionStore));
        }
        public IEnumerable<Permission> GetPermissions(string grain = null, string securableItem = null, string permissionName = null)
        {
            return _permissionStore.GetPermissions(grain, securableItem, permissionName);
        }

        public Permission GetPermission(Guid permissionId)
        {
            return _permissionStore.Get(permissionId);
        }

        public Permission AddPermission(Permission permission)
        {
            return _permissionStore.Add(permission);
        }

        public void DeletePermission(Permission permission)
        {
            _permissionStore.Delete(permission);
        }
    }
}
