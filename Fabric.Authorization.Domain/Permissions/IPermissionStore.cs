using System;
using System.Collections.Generic;
using System.Text;

namespace Fabric.Authorization.Domain.Permissions
{
    public interface IPermissionStore
    {
        IEnumerable<Permission> GetPermissions(string grain = null, string resource = null,
            string permissionName = null);
        
        Permission GetPermission(Guid permissionId);

        void AddPermission(Permission permission);

        void DeletePermission(Permission permission);

        void UpdatePermission(Permission permission);
    }
}
