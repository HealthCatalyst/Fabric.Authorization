using System;
using System.Collections.Generic;

namespace Fabric.Authorization.Domain.Services
{
    public interface IPermissionService
    {
        IEnumerable<Permission> GetPermissions(string grain = null, string resource = null,
            string permissionName = null);

        Permission GetPermission(Guid permissionId);
        
        Permission AddPermission(string grain, string resource, string permissionName);

        Result<Permission> ValidatePermission(string grain, string resource, string permissionName);

        void DeletePermission(Permission permission);
    }
}
