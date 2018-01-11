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
            //validate permissions and remove them from the domain model and add them explicitly
            
            var allowPermissionsToAdd = role.Permissions.Distinct();
            var denyPermissionsToAdd = role.DeniedPermissions.Distinct();

            bool isPermissionListValid;

            isPermissionListValid = allowPermissionsToAdd.Intersect(denyPermissionsToAdd).Any();
             
            //ensure permission already exists and the grain and securable item are correct


            return await _roleStore.Add(role);
        }

        /// <summary>
        /// Removes an existing Role.
        /// </summary>
        public async Task DeleteRole(Role role) => await _roleStore.Delete(role);

        public async Task<Role> AddPermissionsToRole(Role role, Guid[] permissionIds)
        {
            var permissionsToAdd = new List<Permission>();
            foreach (var permissionId in permissionIds)
            {
                if (role.Permissions.Any(p => p.Id == permissionId))
                {
                    throw new AlreadyExistsException<Permission>(
                        $"Permission {permissionId} already exists for role {role.Name}. Please provide a new permission id.");
                }

                var permission = await _permissionStore.Get(permissionId);
                if (permission.Grain == role.Grain && permission.SecurableItem == role.SecurableItem)
                {
                    permissionsToAdd.Add(permission);
                }
                else
                {
                    throw new IncompatiblePermissionException($"Permission with id {permission.Id} has the wrong grain and/or securableItem.");
                }
            }
            var updatedRole = await _roleStore.AddPermissionsToRole(role, permissionsToAdd);
            return updatedRole;
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