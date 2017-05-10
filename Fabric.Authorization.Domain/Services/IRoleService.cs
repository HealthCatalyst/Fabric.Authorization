using System;
using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Services
{
    public interface IRoleService
    {
        IEnumerable<Role> GetRoles(string grain = null, string resource = null, string roleName = null);

        IEnumerable<Permission> GetPermissionsForRole(Guid roleId);

        void AddRole(string grain, string resource, string roleName);

        void DeleteRole(Guid roleId);

        void AddPermissionsToRole(Guid roleId, Guid[] permissionIds);

        void RemovePermissionsFromRole(Guid roleId, Guid[] permissionIds);
    }
}
