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


        /// <summary>
        ///     Constructor.
        /// </summary>
        public PermissionService(IPermissionStore permissionStore, RoleService roleService)
        {
            _permissionStore = permissionStore ?? throw new ArgumentNullException(nameof(permissionStore));
            _roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
        }

        /// <summary>
        ///     Gets all permissions for a given grain/secitem.
        /// </summary>
        public async Task<IEnumerable<Permission>> GetPermissions(string grain = null, string securableItem = null,
            string permissionName = null, bool includeDeleted = false)
        {
            var permissions = await _permissionStore.GetPermissions(grain, securableItem, permissionName);
            return permissions.Where(p => !p.IsDeleted || includeDeleted);
        }

        /// <summary>
        ///     Gets all the permissions associated to the groups through roles.
        /// </summary>
        public async Task<IEnumerable<string>> GetPermissionsForGroups(string[] groupNames, string grain = null,
            string securableItem = null)
        {
            var effectivePermissions = new List<string>();
            var deniedPermissions = new List<string>();

            var roles = await _roleService.GetRoles(grain, securableItem);

            foreach (var role in roles)
            {
                if (role.Groups.Any(groupNames.Contains) && !role.IsDeleted && role.Permissions != null)
                {
                    // Add permissions in current role
                    effectivePermissions.AddRange(role.Permissions.Where(p =>
                            !p.IsDeleted &&
                            (p.Grain == grain || grain == null) &&
                            (p.SecurableItem == securableItem || securableItem == null))
                        .Select(p => p.ToString()));

                    deniedPermissions.AddRange(role.DeniedPermissions.Select(p => p.ToString()));

                    // Add permissions from parent roles
                    var ancestorRoles = _roleService.GetRoleHierarchy(role, roles);
                    foreach (var ancestorRole in ancestorRoles)
                    {
                        effectivePermissions.AddRange(ancestorRole.Permissions.Where(p =>
                                !p.IsDeleted &&
                                (p.Grain == grain || grain == null) &&
                                (p.SecurableItem == securableItem || securableItem == null))
                            .Select(p => p.ToString()));

                        deniedPermissions.AddRange(ancestorRole.DeniedPermissions.Select(p => p.ToString()));
                    }
                }
            }

            // Remove blacklisted permissions and return
            return effectivePermissions.Except(deniedPermissions).Distinct();
        }

        /// <summary>
        ///     Gets all the permissions for a given user.
        /// </summary>
        public async Task<IEnumerable<string>> GetPermissionsForUser(string userId, string[] groupNames,
            string grain = null, string securableItem = null)
        {
            var effectivePermissions = await GetPermissionsForGroups(groupNames, grain, securableItem);

            var additionalPermissions = Enumerable.Empty<string>();
            var deniedPermissions = Enumerable.Empty<string>();

            try
            {
                var granularPermissions = await GetUserGranularPermissions(userId);

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


        /// <summary>
        ///     Adds granular permissions to a user.
        /// </summary>
        public async Task AddUserGranularPermissions(GranularPermission granularPermission)
        {
            try
            {
                var stored = await GetUserGranularPermissions(granularPermission.Id);

                ValidatePermissionsForAdd(granularPermission.AdditionalPermissions,
                    granularPermission.DeniedPermissions,
                    stored.AdditionalPermissions,
                    stored.DeniedPermissions);

                var allowPermsList = granularPermission.AdditionalPermissions.ToList();
                var denyPermsList = granularPermission.DeniedPermissions.ToList();

                allowPermsList.AddRange(stored.AdditionalPermissions);
                denyPermsList.AddRange(stored.DeniedPermissions);

                granularPermission.AdditionalPermissions = allowPermsList;
                granularPermission.DeniedPermissions = denyPermsList;

            }
            catch (NotFoundException<GranularPermission>)
            {
                //nothing to do here. if the user has never had granular permissions stored then save whatever was passed in
            }

            await _permissionStore.AddOrUpdateGranularPermission(granularPermission);
        }

        private void ValidatePermissionsForAdd(IEnumerable<Permission> allowPermissionsToAdd,
            IEnumerable<Permission> denyPermissionsToAdd,
            IEnumerable<Permission> existingAllowPermissions,
            IEnumerable<Permission> existingDenyPermissions)
        {
            var invalidPermissions = new List<KeyValuePair<string,string>>();

            invalidPermissions.AddRange(allowPermissionsToAdd.Intersect(existingAllowPermissions)
                .Select(p => new KeyValuePair<string,string>("Duplicate allow permissions", p.ToString())));
            invalidPermissions.AddRange(denyPermissionsToAdd.Intersect(existingDenyPermissions)
                .Select(p => new KeyValuePair<string, string>("Duplicate deny permissions", p.ToString() )));

            invalidPermissions.AddRange(allowPermissionsToAdd.Intersect(existingDenyPermissions)
                .Select(p => new KeyValuePair<string, string>("Exists as deny", p.ToString() )));
            invalidPermissions.AddRange(denyPermissionsToAdd.Intersect(existingAllowPermissions)
                .Select(p => new KeyValuePair<string, string>("Exists as allow", p.ToString())));

            if (invalidPermissions.Any())
            {
                var invalidPermissionException =
                    new InvalidPermissionException("Cannot add the specified permissions, please correct the issues and attempt to add again. The following permissions are invalid:");

                var permissionGroups = invalidPermissions.GroupBy(i => i.Key);

                foreach(var group in permissionGroups)
                {
                    invalidPermissionException.Data.Add(group.Key, string.Join(", ", group.Select(p => p.Value)));
                }

                throw invalidPermissionException;
            }
        }

        /// <summary>
        /// removes granular permissions from a user
        /// </summary>
        /// <param name="granularPermission"></param>
        /// <returns></returns>
        public async Task DeleteGranularPermissions(GranularPermission granularPermission)
        {
            try
            {
                var stored = await GetUserGranularPermissions(granularPermission.Id);

                //ensure every permission passed in belongs to the user or dont update anything - get a list of invalid permissions
                ValidatePermissionsForDelete(granularPermission.AdditionalPermissions, 
                    granularPermission.DeniedPermissions, 
                    stored.AdditionalPermissions, 
                    stored.DeniedPermissions);

                stored.AdditionalPermissions = stored.AdditionalPermissions.Where(p => !granularPermission.AdditionalPermissions.Contains(p));
                stored.DeniedPermissions = stored.DeniedPermissions.Where(p => !granularPermission.DeniedPermissions.Contains(p));

                await _permissionStore.AddOrUpdateGranularPermission(stored);
            }
            catch (NotFoundException<GranularPermission>)
            {
                ValidatePermissionsForDelete(granularPermission.AdditionalPermissions,
                    granularPermission.DeniedPermissions,
                    null,
                    null);
            }                             
        }

        private void ValidatePermissionsForDelete(IEnumerable<Permission> allowPermissionsToDelete, 
            IEnumerable<Permission> denyPermissionsToDelete, 
            IEnumerable<Permission> existingAllowPermissions, 
            IEnumerable<Permission> existingDenyPermissions)
        {
            var invalidPermissionActionAllowPermissions = existingDenyPermissions?
                .Where(p => allowPermissionsToDelete.Contains(p)) ?? Enumerable.Empty<Permission>();

            var invalidAllowPermissions = allowPermissionsToDelete
                .Except(existingAllowPermissions ?? Enumerable.Empty<Permission>())
                .Except(invalidPermissionActionAllowPermissions);            

            var invalidPermissionActionDenyPermissions = existingAllowPermissions?
                .Where(p => denyPermissionsToDelete.Contains(p)) ?? Enumerable.Empty<Permission>();

            var invalidDenyPermissions = denyPermissionsToDelete
                .Except(existingDenyPermissions ?? Enumerable.Empty<Permission>())
                .Except(invalidPermissionActionDenyPermissions);            

            if(invalidAllowPermissions.Any() 
                || invalidDenyPermissions.Any() 
                || invalidPermissionActionAllowPermissions.Any() 
                || invalidPermissionActionDenyPermissions.Any())
            {
                var invalidPermissionException = 
                    new InvalidPermissionException("Cannot delete the specified permissions, please correct the issues and attempt to delete again. The following permissions are invalid:");

                if (invalidAllowPermissions.Any())
                {
                    invalidPermissionException.Data.Add("Invalid allow permissions", string.Join(",", invalidAllowPermissions));
                }
                if (invalidDenyPermissions.Any())
                {
                    invalidPermissionException.Data.Add("Invalid deny permissions", string.Join(",", invalidDenyPermissions));
                }
                if(invalidPermissionActionAllowPermissions.Any())
                {
                    invalidPermissionException.Data.Add("Invalid allow permission actions", string.Join(",", invalidPermissionActionAllowPermissions));
                }
                if (invalidPermissionActionDenyPermissions.Any())
                {
                    invalidPermissionException.Data.Add("Invalid deny permission actions", string.Join(",", invalidPermissionActionDenyPermissions));
                }

                throw invalidPermissionException;
            }
        }

        /// <summary>
        ///     Gets the granular permissions for a user.
        /// </summary>
        public async Task<GranularPermission> GetUserGranularPermissions(string userId)
        {
            return await _permissionStore.GetGranularPermission(userId);
        }

        /// <summary>
        ///     Get a single permission by Id.
        /// </summary>
        public async Task<Permission> GetPermission(Guid permissionId)
        {
            return await _permissionStore.Get(permissionId);
        }

        /// <summary>
        ///     Add a single permission.
        /// </summary>
        public async Task<Permission> AddPermission(Permission permission)
        {
            return await _permissionStore.Add(permission);
        }

        /// <summary>
        ///     Removes a single permission.
        ///     This both removes the permission from the db, and also removes the permission from all the roles that contain the
        ///     permission.
        /// </summary>
        public async Task DeletePermission(Permission permission)
        {
            await _roleService.RemovePermissionsFromRoles(permission.Id, permission.Grain, permission.SecurableItem);
            await _permissionStore.Delete(permission);
        }
    }
}