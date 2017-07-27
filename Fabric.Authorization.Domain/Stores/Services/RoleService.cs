using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores.Services
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

        public async Task<IEnumerable<Role>> GetRoles(string grain = null, string securableItem = null, string roleName = null, bool includeDeleted = false)
        {
            var roles = await _roleStore.GetRoles(grain, securableItem, roleName);
            var permissions = await _permissionStore.GetPermissions(grain, securableItem);
            var rolesToReturn = new List<Role>();
            foreach (var role in roles.Where(r => !r.IsDeleted || includeDeleted))
            {
                role.Permissions = GetPermissionsFromRole(role, permissions).ToList();
                rolesToReturn.Add(role);
            }
            return rolesToReturn;
        }

        public async Task<Role> GetRole(Guid roleId)
        {
            var role = await _roleStore.Get(roleId);
            var permissions = await _permissionStore.GetPermissions(role.Grain, role.SecurableItem);
            role.Permissions = GetPermissionsFromRole(role, permissions).ToList();
            return role;
        }

        public async Task<IEnumerable<Permission>> GetPermissionsForRole(Guid roleId)
        {
            var role = await _roleStore.Get(roleId);
            var permissions = await _permissionStore.GetPermissions(role.Grain, role.SecurableItem);
            return GetPermissionsFromRole(role, permissions);
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
        
        public IEnumerable<Role> GetRoleHierarchy(Role role, IEnumerable<Role> roles)
        {
            var ancestorRoles = new List<Role>();
            if (role.ParentRole.HasValue && roles.Any(r => r.Id == role.ParentRole && !r.IsDeleted))
            {
                var ancestorRole = roles.First(r => r.Id == role.ParentRole && !r.IsDeleted);
                ancestorRoles.Add(ancestorRole);
                ancestorRoles.AddRange(GetRoleHierarchy(ancestorRole, roles));
            }
            return ancestorRoles;
        }

        private IEnumerable<Permission> GetPermissionsFromRole(Role role, IEnumerable<Permission> permissions)
        {
            var permissionIds = role.Permissions.Select(p => p.Id);
            return permissions.Where(p => permissionIds.Contains(p.Id) && !p.IsDeleted).ToList();
        }
    }
}