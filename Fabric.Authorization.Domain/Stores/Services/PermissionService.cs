using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;

namespace Fabric.Authorization.Domain.Services
{
    public class PermissionService
    {
        private readonly IPermissionStore _permissionStore;

        public PermissionService(IPermissionStore permissionStore)
        {
            _permissionStore = permissionStore ?? throw new ArgumentNullException(nameof(permissionStore));
        }

        public async Task<IEnumerable<Permission>> GetPermissions(string grain = null, string securableItem = null, string permissionName = null, bool includeDeleted = false)
        {
            var permissions = await _permissionStore.GetPermissions(grain, securableItem, permissionName);
            return permissions.Where(p => !p.IsDeleted || includeDeleted);
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