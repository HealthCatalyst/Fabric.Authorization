using System;
using System.Collections.Generic;
using System.Text;

namespace Fabric.Authorization.Domain.Permissions
{
    public interface IPermissionService
    {
        IEnumerable<Permission> GetPermissions(string grain = null, string resource = null,
            string permissionName = null);
        
        void AddPermission(string grain, string resource, string permissionName);

        void DeletePermission(Guid permissionId);
    }
}
