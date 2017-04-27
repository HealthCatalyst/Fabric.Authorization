using System;
using System.Collections.Generic;
using System.Text;

namespace Fabric.Authorization.Domain.Roles
{
    public interface IRoleService
    {
        IEnumerable<Role> GetRoles(string grain = null, string resource = null, string roleName = null);

        IEnumerable<Permission> GetPermissionsForRole(int roleId);

        void AddRole(string grain, string resource, string roleName);

        void DeleteRole(Guid roleId);

        void AddPermissionsToRole(Guid[] permissionIds);

        void RemovePermissionsFromRole(Guid[] permissionIds);
    }
}
