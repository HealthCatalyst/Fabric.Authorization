using System;
using System.Collections.Generic;
using System.Linq;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;

namespace Fabric.Authorization.Domain.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRoleStore _roleStore;
        private readonly IPermissionStore _permissionStore;

        public RoleService(IRoleStore roleStore, IPermissionStore permissionStore)
        {
            _roleStore = roleStore ?? throw new ArgumentNullException(nameof(roleStore));
            _permissionStore = permissionStore ?? throw new ArgumentNullException(nameof(permissionStore));
        }
        public IEnumerable<Role> GetRoles(string grain = null, string securableItem = null, string roleName = null)
        {
            return _roleStore.GetRoles(grain, securableItem, roleName);
        }

        public Role GetRole(Guid roleId)
        {
            return _roleStore.GetRole(roleId);
        }

        public IEnumerable<Permission> GetPermissionsForRole(Guid roleId)
        {
            var role = _roleStore.GetRole(roleId);
            return role.Permissions;
        }

        public Role AddRole(Role role)
        {
            return _roleStore.AddRole(role);
        }

        public void DeleteRole(Role role)
        {
            _roleStore.DeleteRole(role);
        }

        public Role AddPermissionsToRole(Role role, Guid[] permissionIds)
        {
            var permissionsToAdd = new List<Permission>();
            foreach (var permissionId in permissionIds)
            {
                var permission = _permissionStore.GetPermission(permissionId);
                if (permission.Grain == role.Grain && permission.SecurableItem == role.SecurableItem && role.Permissions.All(p => p.Id != permission.Id))
                {
                    permissionsToAdd.Add(permission);
                }
                else
                {
                    throw new IncompatiblePermissionException($"Permission with id {permission.Id} has the wrong grain, securableItem, or is already present on the role");
                }
            }
            foreach (var permission in permissionsToAdd)
            {
                role.Permissions.Add(permission);
            }
            _roleStore.UpdateRole(role);
            return role;
        }

        public Role RemovePermissionsFromRole(Role role, Guid[] permissionIds)
        {
            foreach (var permissionId in permissionIds)
            {
                if (role.Permissions.All(p => p.Id != permissionId))
                {
                    throw new PermissionNotFoundException($"Permission with id {permissionId} not found on role {role.Id}");
                }
            }
            foreach (var permissionId in permissionIds)
            {
                var permission = _permissionStore.GetPermission(permissionId);
                role.Permissions.Remove(permission);
            }
            _roleStore.UpdateRole(role);
            return role;
        }
    }
}
