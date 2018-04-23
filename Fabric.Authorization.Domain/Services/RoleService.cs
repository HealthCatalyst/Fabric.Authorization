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
                try
                {
                    if (await _clientService.DoesClientOwnItem(client.Id, role.Grain, role.SecurableItem))
                    {
                        clientRoles.Add(role);
                    }
                }
                catch (NotFoundException<SecurableItem>)
                {
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
            //validate permissions and remove them from the domain model and add them explicitly

            var allowPermissionsToAdd = role.Permissions.Distinct().ToList();
            var denyPermissionsToAdd = role.DeniedPermissions.Distinct().ToList();
            role.Permissions = new List<Permission>();
            role.DeniedPermissions = new List<Permission>();
            try
            {
                var allowPermissions = await ValidatePermissionList(allowPermissionsToAdd.Select(p => p.Id), role.Name,
                    role.Grain, role.SecurableItem, Enumerable.Empty<Permission>());
                var denyPermissions = await ValidatePermissionList(denyPermissionsToAdd.Select(p => p.Id), role.Name,
                    role.Grain, role.SecurableItem, Enumerable.Empty<Permission>());

                await ValidateChildRoles(role.ChildRoles, role.Grain, role.SecurableItem);

                var newRole = await _roleStore.Add(role);
                await _roleStore.AddPermissionsToRole(newRole, allowPermissions, denyPermissions);
                return newRole;
            }
            catch (AlreadyExistsException<Permission> e)
            {
                throw new BadRequestException<Permission>(e.Message);
            }
            catch (IncompatiblePermissionException e)
            {
                throw new BadRequestException<Permission>(e.Message);
            }
        }

        public async Task<Role> UpdateRole(Role role)
        {
            try
            {
                await _roleStore.Update(role);
                return role;
            }
            catch (NotFoundException<Role> e)
            {
                throw new NotFoundException<Role>(e.Message);
            }
        }

        /// <summary>
        /// Removes an existing Role.
        /// </summary>
        public async Task DeleteRole(Role role) => await _roleStore.Delete(role);

        public async Task<Role> AddPermissionsToRole(Role role, Guid[] allowPermissionIds, Guid[] denyPermissionIds)
        {
            var permissionsToAdd = await ValidatePermissionList(allowPermissionIds, role.Name, role.Grain, role.SecurableItem, role.Permissions);
            var denyPermissionsToAdd = await ValidatePermissionList(denyPermissionIds, role.Name, role.Grain, role.SecurableItem, role.DeniedPermissions);

            var updatedRole = await _roleStore.AddPermissionsToRole(role, permissionsToAdd, denyPermissionsToAdd);
            return updatedRole;
        }

        private async Task<List<Permission>> ValidatePermissionList(IEnumerable<Guid> permissionIds, string roleName, string grain, string securableItem, IEnumerable<Permission> existingPermissions)
        {
            var permissionsToAdd = new List<Permission>();
            var permissions = existingPermissions.ToList();
            foreach (var permissionId in permissionIds)
            {
                if (permissions.Any(p => p.Id == permissionId))
                {
                    throw new AlreadyExistsException<Permission>(
                        $"Permission {permissionId} already exists for role {roleName}. Please provide a new permission id.");
                }

                var permission = await _permissionStore.Get(permissionId);
                if (!(permission.Grain == grain && permission.SecurableItem == securableItem))
                {
                    throw new IncompatiblePermissionException(
                        $"Permission with id {permission.Id} has the wrong grain and/or securableItem.");
                }

                permissionsToAdd.Add(permission);
            }
            return permissionsToAdd;
        }

        private async Task ValidateChildRoles(IEnumerable<Guid> childRoles, string grain, string securableItem)
        {
            var exceptions = new List<Exception>();
            foreach (var childRole in childRoles)
            {
                try
                {
                    var role = await _roleStore.Get(childRole);
                    if (!(role.Grain == grain && role.SecurableItem == securableItem))
                    {
                        exceptions.Add(new IncompatibleRoleException($"Role with id {role.Id} has the wrong grain and/or securableItem."));
                    }
                }
                catch (NotFoundException<Role> ex)
                {
                    exceptions.Add(ex);
                }

            }

            if (exceptions.Count > 0)
            {
                throw new AggregateException("There was an issue adding a child role to a role. See the inner exceptions for details", exceptions);
            }
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

            var updatedRole = await _roleStore.RemovePermissionsFromRole(role, permissionIds);
            return updatedRole;
        }

        public async Task RemovePermissionsFromRoles(Guid permissionId, string grain, string securableItem = null)
        {
            await _roleStore.RemovePermissionFromRoles(permissionId, grain, securableItem);
        }

        /// <summary>
        /// Gets the topological sort of a role graph.
        /// </summary>
        public IEnumerable<Role> GetRoleHierarchy(Role role, ICollection<Role> roles)
        {
            var ancestorRoles = new List<Role>();
            if (!role.ParentRole.HasValue || !roles.Any(r => r.Id == role.ParentRole && !r.IsDeleted))
            {
                return ancestorRoles;
            }
            var ancestorRole = roles.First(r => r.Id == role.ParentRole && !r.IsDeleted);
            ancestorRoles.Add(ancestorRole);
            ancestorRoles.AddRange(GetRoleHierarchy(ancestorRole, roles));
            return ancestorRoles;
        }
    }
}