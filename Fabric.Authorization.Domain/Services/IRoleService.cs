using System;
using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Services
{
    public interface IRoleService
    {
        IEnumerable<Role> GetRoles(string grain = null, string securableItem = null, string roleName = null);

        Role GetRole(Guid roleId);

        IEnumerable<Permission> GetPermissionsForRole(Guid roleId);

        Role AddRole(Role role);

        void DeleteRole(Role role);

        Role AddPermissionsToRole(Role role, Guid[] permissionIds);

        Role RemovePermissionsFromRole(Role role, Guid[] permissionIds);
    }
}
