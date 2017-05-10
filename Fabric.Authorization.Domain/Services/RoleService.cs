using System;
using System.Collections.Generic;
using System.Linq;
using Fabric.Authorization.Domain.Exceptions;
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
        public IEnumerable<Role> GetRoles(string grain = null, string resource = null, string roleName = null)
        {
            return _roleStore.GetRoles(grain, resource, roleName);
        }

        public IEnumerable<Permission> GetPermissionsForRole(Guid roleId)
        {
            var role = _roleStore.GetRole(roleId);
            return role.Permissions;
        }

        public void AddRole(string grain, string resource, string roleName)
        {
            if (_roleStore.GetRoles(grain, resource, roleName).Any())
            {
                throw new RoleAlreadyExistsException();
            }
            _roleStore.AddRole(new Role
            {
                Grain = grain,
                Resource = resource,
                Name = roleName
            });
        }

        public void DeleteRole(Guid roleId)
        {
            var role = _roleStore.GetRole(roleId);
            _roleStore.DeleteRole(role);
        }

        public void AddPermissionsToRole(Guid roleId, Guid[] permissionIds)
        {
            var role = _roleStore.GetRole(roleId);
            var permissionsToAdd = new List<Permission>();
            foreach (var permissionId in permissionIds)
            {
                var permission = _permissionStore.GetPermission(permissionId);
                if (permission.Grain == role.Grain && permission.Resource == role.Resource && role.Permissions.All(p => p.Id != permission.Id))
                {
                    permissionsToAdd.Add(permission);
                }
                else
                {
                    throw new IncompatiblePermissionException($"Permission with id {permission.Id} has the wrong grain, resource or is already present on the role");
                }
            }
            foreach (var permission in permissionsToAdd)
            {
                role.Permissions.Add(permission);
            }
            _roleStore.UpdateRole(role);
        }

        public void RemovePermissionsFromRole(Guid roleId, Guid[] permissionIds)
        {
            var role = _roleStore.GetRole(roleId);
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
        }
    }
}
