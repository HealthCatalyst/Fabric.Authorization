using System;
using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Services
{
    public interface IRoleService
    {
        IEnumerable<Role> GetRoles(string grain = null, string securableItem = null, string roleName = null);

        IEnumerable<Permission> GetPermissionsForRole(Guid roleId);

        Role AddRole(Role role);

        void DeleteRole(Role role);

        void AddPermissionsToRole(Guid roleId, Guid[] permissionIds);

        void RemovePermissionsFromRole(Guid roleId, Guid[] permissionIds);
    }
}
