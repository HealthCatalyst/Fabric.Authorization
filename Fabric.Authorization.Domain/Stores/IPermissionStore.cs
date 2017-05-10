using System;
using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores
{
    public interface IPermissionStore
    {
        IEnumerable<Permission> GetPermissions(string grain = null, string securableItem = null,
            string permissionName = null);
        
        Permission GetPermission(Guid permissionId);

        Permission AddPermission(Permission permission);

        void DeletePermission(Permission permission);

        void UpdatePermission(Permission permission);
    }
}
