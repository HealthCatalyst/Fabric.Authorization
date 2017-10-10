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
        private readonly ClientService _clientService;

        /// <summary>
        /// Constructor
        /// </summary>
        public RoleService(IRoleStore roleStore, IPermissionStore permissionStore)
        {
            _roleStore = roleStore ?? throw new ArgumentNullException(nameof(roleStore));
            _permissionStore = permissionStore ?? throw new ArgumentNullException(nameof(permissionStore));
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public RoleService(IRoleStore roleStore, IPermissionStore permissionStore, ClientService clientService)
        {
            _roleStore = roleStore ?? throw new ArgumentNullException(nameof(roleStore));
            _permissionStore = permissionStore ?? throw new ArgumentNullException(nameof(permissionStore));
            _clientService = clientService ?? throw new ArgumentNullException(nameof(clientService));
        }

        /// <summary>
        /// Gets all roles for a grain / secitem
        /// </summary>
        public async Task<IEnumerable<Role>> GetRoles(string grain = null, string securableItem = null, string roleName = null, bool includeDeleted = false)
        {
            var roles = await _roleStore.GetRoles(grain, securableItem, roleName);
            return roles.Where(r => !r.IsDeleted || includeDeleted);
        }

        /// <summary>
        /// Gets all roles owned by the specified <paramref name="client"/>.
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Role>> GetRoles(Client client)
        {
            var clientRoles = new List<Role>();
            var roles = await GetRoles();

            foreach (var role in roles)
            {
                if (_clientService.DoesClientOwnItem(client.TopLevelSecurableItem, role.Grain, role.SecurableItem))
                {
                    clientRoles.Add(role);
                }
            }

            return clientRoles;
        }

        public async Task<IEnumerable<Role>> GetRoles(string clientId)
        {
            var client = await _clientService.GetClient(clientId);
            return await GetRoles(client);
        }

        /// <summary>
        /// Gets a role by Id.
        /// </summary>
        public async Task<Role> GetRole(Guid roleId)
        {
            return await _roleStore.Get(roleId);
        }

        /// <summary>
        /// Get permissions associated with a role.
        /// </summary>
        public async Task<IEnumerable<Permission>> GetPermissionsForRole(Guid roleId)
        {
            var role = await _roleStore.Get(roleId);
            var permissions = role.Permissions;

            // TODO: check if we can remove the ToList
            return permissions.Where(p => !p.IsDeleted).ToList();
        }

        /// <summary>
        /// Creates a new Role.
        /// </summary>
        public async Task<Role> AddRole(Role role)
        {
            return await _roleStore.Add(role);
        }

        /// <summary>
        /// Removes an existing Role.
        /// </summary>
        public async Task DeleteRole(Role role) => await _roleStore.Delete(role);

        /// <summary>
        /// Adds permissions to a Role.
        /// </summary>
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

        /// <summary>
        /// Removes permissions from a Role.
        /// </summary>
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
                var permission = role.Permissions.First(p => p.Id == permissionId);
                role.Permissions.Remove(permission);
            }

            await _roleStore.Update(role);
            return role;
        }

        /// <summary>
        /// Removes a permission from all roles.
        /// </summary>
        public async Task RemovePermissionsFromRoles(Guid permissionId, string grain, string securableItem = null)
        {
            var roles = await _roleStore.GetRoles(grain, securableItem);

            foreach (var role in roles)
            {
                if (role.Permissions != null && role.Permissions.Any(p => p.Id == permissionId))
                {
                    var permission = role.Permissions.First(p => p.Id == permissionId);
                    role.Permissions.Remove(permission);
                    await _roleStore.Update(role);
                }
            }
        }

        /// <summary>
        /// Gets the topological sort of a role graph.
        /// </summary>
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
    }
}