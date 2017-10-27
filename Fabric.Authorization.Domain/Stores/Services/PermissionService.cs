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

        /// <summary>
        ///     Gets all permissions for a given grain/secitem.
        /// </summary>
        public async Task<IEnumerable<Permission>> GetPermissions(
            string grain = null,
            string securableItem = null,
            string permissionName = null,
            bool includeDeleted = false)
        {
            var permissions = await _permissionStore.GetPermissions(grain, securableItem, permissionName);
            return permissions.Where(p => !p.IsDeleted || includeDeleted);
        }

        /// <summary>
        ///     Adds granular permissions to a user.
        /// </summary>
        public async Task AddUserGranularPermissions(GranularPermission granularPermission)
        {
            try
            {
                var stored = await GetUserGranularPermissions(granularPermission.Id);

                ValidatePermissionsForAdd(
                    granularPermission.AdditionalPermissions,
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
                ValidatePermissionsForAdd(
                    granularPermission.AdditionalPermissions,
                    granularPermission.DeniedPermissions,
                    null,
                    null);
            }

            await _permissionStore.AddOrUpdateGranularPermission(granularPermission);
        }

        private static void ValidatePermissionsForAdd(
            IEnumerable<Permission> allowPermissionsToAdd,
            IEnumerable<Permission> denyPermissionsToAdd,
            IEnumerable<Permission> existingAllowPermissions,
            IEnumerable<Permission> existingDenyPermissions)
        {
            var invalidPermissions = new List<KeyValuePair<string, string>>();

            invalidPermissions.AddRange(allowPermissionsToAdd
                .Intersect(existingAllowPermissions ?? Enumerable.Empty<Permission>())
                .Select(p => new KeyValuePair<string, string>(
                    "The following permissions already exist as 'allow' permissions", p.ToString())));

            invalidPermissions.AddRange(denyPermissionsToAdd
                .Intersect(existingDenyPermissions ?? Enumerable.Empty<Permission>())
                .Select(p => new KeyValuePair<string, string>(
                    "The following permissions already exist as 'deny' permissions", p.ToString())));

            invalidPermissions.AddRange(allowPermissionsToAdd
                .Intersect(existingDenyPermissions ?? Enumerable.Empty<Permission>())
                .Select(p => new KeyValuePair<string, string>(
                    "The following permissions exist as 'deny' and cannot be added as 'allow'", p.ToString())));

            invalidPermissions.AddRange(denyPermissionsToAdd
                .Intersect(existingAllowPermissions ?? Enumerable.Empty<Permission>())
                .Select(p => new KeyValuePair<string, string>(
                    "The following permissions exist as 'allow' and cannot be added as 'deny'", p.ToString())));

            invalidPermissions.AddRange(allowPermissionsToAdd.Intersect(denyPermissionsToAdd)
                .Select(p => new KeyValuePair<string, string>(
                    "The following permissions cannot be specified as both 'allow' and 'deny'", p.ToString())));

            if (!invalidPermissions.Any())
            {
                return;
            }

            var invalidPermissionException =
                new InvalidPermissionException(
                    "Cannot add the specified permissions, please correct the issues and attempt to add again.");

            var permissionGroups = invalidPermissions.GroupBy(i => i.Key);

            foreach (var group in permissionGroups)
            {
                invalidPermissionException.Data.Add(group.Key, string.Join(", ", group.Select(p => p.Value)));
            }

            throw invalidPermissionException;
        }

        /// <summary>
        ///     removes granular permissions from a user
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

                stored.AdditionalPermissions =
                    stored.AdditionalPermissions.Where(p => !granularPermission.AdditionalPermissions.Contains(p));
                stored.DeniedPermissions =
                    stored.DeniedPermissions.Where(p => !granularPermission.DeniedPermissions.Contains(p));

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

        private static void ValidatePermissionsForDelete(
            IEnumerable<Permission> allowPermissionsToDelete,
            IEnumerable<Permission> denyPermissionsToDelete,
            IEnumerable<Permission> existingAllowPermissions,
            IEnumerable<Permission> existingDenyPermissions)
        {
            var existingAllow = existingAllowPermissions ?? Enumerable.Empty<Permission>();
            var existingDeny = existingDenyPermissions ?? Enumerable.Empty<Permission>();

            var invalidPermissions = new List<KeyValuePair<string, string>>();

            var invalidPermissionActionAllowPermissions = existingDeny
                .Where(p => allowPermissionsToDelete.Contains(p));

            invalidPermissions.AddRange(invalidPermissionActionAllowPermissions
                .Select(p => new KeyValuePair<string, string>(
                    "The following permissions exist as 'deny' for user but 'allow' was specified", p.ToString())));

            invalidPermissions.AddRange(allowPermissionsToDelete
                .Except(existingAllow)
                .Except(invalidPermissionActionAllowPermissions)
                .Select(p => new KeyValuePair<string, string>(
                    "The following permissions do not exist as 'allow' permissions", p.ToString())));

            var invalidPermissionActionDenyPermissions = existingAllow
                .Where(p => denyPermissionsToDelete.Contains(p));

            invalidPermissions.AddRange(invalidPermissionActionDenyPermissions
                .Select(p => new KeyValuePair<string, string>(
                    "The following permissions exist as 'allow' for user but 'deny' was specified", p.ToString())));

            invalidPermissions.AddRange(denyPermissionsToDelete
                .Except(existingDeny)
                .Except(invalidPermissionActionDenyPermissions)
                .Select(p => new KeyValuePair<string, string>(
                    "The following permissions do not exist as 'deny' permissions", p.ToString())));


            if (!invalidPermissions.Any())
            {
                return;
            }

            var invalidPermissionException =
                new InvalidPermissionException(
                    "Cannot delete the specified permissions, please correct the issues and attempt to delete again.");

            var permissionGroups = invalidPermissions.GroupBy(i => i.Key);

            foreach (var group in permissionGroups)
            {
                invalidPermissionException.Data.Add(group.Key, string.Join(", ", group.Select(p => p.Value)));
            }

            throw invalidPermissionException;
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