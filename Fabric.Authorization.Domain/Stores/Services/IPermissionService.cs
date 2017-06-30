using System;
using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Services
{
    public interface IPermissionService
    {
        IEnumerable<Permission> GetPermissions(string grain = null, string securableItem = null,
            string permissionName = null);

        Permission GetPermission(Guid permissionId);
        
        Permission AddPermission(Permission permission);
        
        void DeletePermission(Permission permission);
    }
}
