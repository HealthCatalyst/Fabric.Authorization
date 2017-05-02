using System;
using System.Collections.Generic;

namespace Fabric.Authorization.Domain.Permissions
{
    public interface IPermissionService
    {
        IEnumerable<Permission> GetPermissions(string grain = null, string resource = null,
            string permissionName = null);

        Permission GetPermission(Guid permissionId);
        
        Result<T> AddPermission<T>(string grain, string resource, string permissionName);

        void DeletePermission(Guid permissionId);
    }
}
