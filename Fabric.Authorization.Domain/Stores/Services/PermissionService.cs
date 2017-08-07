using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores.Services
{
    public class PermissionService
    {
        private readonly IPermissionStore _permissionStore;
        private readonly RoleService _roleService;

        public PermissionService(IPermissionStore permissionStore, RoleService roleService)
        {
            _permissionStore = permissionStore ?? throw new ArgumentNullException(nameof(permissionStore));
            _roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
        }

        public async Task<IEnumerable<Permission>> GetPermissions(string grain = null, string securableItem = null, string permissionName = null, bool includeDeleted = false)
        {
            var permissions = await _permissionStore.GetPermissions(grain, securableItem, permissionName);
            return permissions.Where(p => !p.IsDeleted || includeDeleted);
        }

        /// <summary>
        /// Gets all the permissions associated to the groups through roles.
        /// </summary>
        public async Task<IEnumerable<string>> GetPermissionsForGroups(string[] groupNames, string grain = null, string securableItem = null)
        {
            var effectivePermissions = new List<string>();
            var deniedPermissions = new List<string>();

            var roles = await _roleService.GetRoles(grain, securableItem);
            var permissions = await _permissionStore.GetPermissions(grain, securableItem);

            foreach (var role in roles)
            {
                if (role.Groups.Any(groupNames.Contains) && !role.IsDeleted && role.Permissions != null)
                {
                    // Add permissions in current role
                    effectivePermissions.AddRange(permissions.Where(p =>
                                    !p.IsDeleted &&
                                    (p.Grain == grain || grain == null) &&
                                    (p.SecurableItem == securableItem || securableItem == null) &&
                                    role.Permissions.Any(rp => rp.Id == p.Id))
                                    .Select(p => p.ToString()));

                    deniedPermissions.AddRange(role.DeniedPermissions.Select(p => p.ToString()));

                    // Add permissions from parent roles
                    var ancestorRoles = _roleService.GetRoleHierarchy(role, roles);
                    foreach (var ancestorRole in ancestorRoles)
                    {
                        effectivePermissions.AddRange(permissions.Where(p =>
                            !p.IsDeleted &&
                            (p.Grain == grain || grain == null) &&
                            (p.SecurableItem == securableItem || securableItem == null) &&
                            ancestorRole.Permissions.Any(rp => rp.Id == p.Id)).Select(p => p.ToString()));

                        deniedPermissions.AddRange(ancestorRole.DeniedPermissions.Select(p => p.ToString()));
                    }
                }
            }

            // Remove blacklisted permissions and return
            return effectivePermissions.Except(deniedPermissions).Distinct();
        }

        /// <summary>
        /// Gets all the permissions associated to the groups through roles.
        /// </summary>
        public async Task<IEnumerable<string>> GetPermissionsForUser(string userId, string[] groupNames, string grain = null, string securableItem = null)
        {
            var effectivePermissions = await this.GetPermissionsForGroups(groupNames, grain, securableItem);
            var additionalPermissions = Enumerable.Empty<string>();
            var deniedPermissions = Enumerable.Empty<string>();

            try
            {
                var granularPermissions = await this.GetUserGranularPermissions(userId);

                if (granularPermissions.AdditionalPermissions != null)
                {
                    additionalPermissions = granularPermissions.AdditionalPermissions.Select(p => p.ToString());
                }

                if (granularPermissions.DeniedPermissions != null)
                {
                    deniedPermissions = granularPermissions.DeniedPermissions.Select(p => p.ToString());
                }
            }
            catch (NotFoundException<GranularPermission>)
            {
                // No granular permissions.
            }

            return effectivePermissions
                .Union(additionalPermissions)
                .Except(deniedPermissions);
        }

        public async Task AddUserGranularPermissions(GranularPermission granularPermission)
        {
            try
            {
                var stored = await this.GetUserGranularPermissions(granularPermission.Target);

                if (granularPermission.AdditionalPermissions == null)
                {
                    granularPermission.AdditionalPermissions = stored.AdditionalPermissions;
                }

                if (granularPermission.DeniedPermissions == null)
                {
                    granularPermission.DeniedPermissions = stored.DeniedPermissions;
                }

            }
            catch (NotFoundException<GranularPermission>)
            {
                // Do nothing
            }

            await _permissionStore.AddOrUpdateGranularPermission(granularPermission);
        }

        public async Task<GranularPermission> GetUserGranularPermissions(string userId)
        {
            return await _permissionStore.GetGranularPermission(userId);
        }

        public async Task<Permission> GetPermission(Guid permissionId)
        {
            return await _permissionStore.Get(permissionId);
        }

        public async Task<Permission> AddPermission(Permission permission)
        {
            return await _permissionStore.Add(permission);
        }

        public async Task DeletePermission(Permission permission) => await _permissionStore.Delete(permission);
    }
}