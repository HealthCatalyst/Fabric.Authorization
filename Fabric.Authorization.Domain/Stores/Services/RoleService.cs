using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;

namespace Fabric.Authorization.Domain.Services
{
    public class RoleService
    {
        private readonly IRoleStore _roleStore;
        private readonly IPermissionStore _permissionStore;

        public RoleService(IRoleStore roleStore, IPermissionStore permissionStore)
        {
            _roleStore = roleStore ?? throw new ArgumentNullException(nameof(roleStore));
            _permissionStore = permissionStore ?? throw new ArgumentNullException(nameof(permissionStore));
        }

        public async Task<IEnumerable<Role>> GetRoles(string grain = null, string securableItem = null, string roleName = null)
        {
            return await _roleStore.GetRoles(grain, securableItem, roleName);
        }

        public async Task<Role> GetRole(Guid roleId)
        {
            return await _roleStore.Get(roleId);
        }

        public async Task<IEnumerable<Permission>> GetPermissionsForRole(Guid roleId)
        {
            var role = await _roleStore.Get(roleId);
            return role.Permissions;
        }

        public async Task<Role> AddRole(Role role)
        {
            return await _roleStore.Add(role);
        }

        public async Task DeleteRole(Role role) => await _roleStore.Delete(role);

        public async Task<Role> AddPermissionsToRole(Role role, Guid[] permissionIds)
        {
            var permissionsToAdd = new List<Permission>();
            foreach (var permissionId in permissionIds)
            {
                var permission = await _permissionStore.Get(permissionId);
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

            await _roleStore.Update(role);
            return role;
        }

        public async Task<Role> RemovePermissionsFromRole(Role role, Guid[] permissionIds)
        {
            foreach (var permissionId in permissionIds)
            {
                if (role.Permissions.All(p => p.Id != permissionId))
                {
                    throw new NotFoundException<Permission>($"Permission with id {permissionId} not found on role {role.Id}");
                }
            }
            foreach (var permissionId in permissionIds)
            {
                var permission = await _permissionStore.Get(permissionId);
                role.Permissions.Remove(permission);
            }

            await _roleStore.Update(role);
            return role;
        }

        public async Task<IEnumerable<Role>> GetRoleHierarchy(Guid roleId) => await _roleStore.GetRoleHierarchy(roleId);
    }
}