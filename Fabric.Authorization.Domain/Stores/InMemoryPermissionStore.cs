using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores
{
    public class InMemoryPermissionStore : IPermissionStore
    {
        private static readonly ConcurrentDictionary<Guid, Permission> Permissions = new ConcurrentDictionary<Guid, Permission>();
        public IEnumerable<Permission> GetPermissions(string grain = null, string resource = null, string permissionName = null)
        {
            var permissions = Permissions.Select(kvp => kvp.Value);
            if (!string.IsNullOrEmpty(grain))
            {
                permissions = permissions.Where(p => p.Grain == grain);
            }
            if (!string.IsNullOrEmpty(resource))
            {
                permissions = permissions.Where(p => p.Resource == resource);
            }
            if (!string.IsNullOrEmpty(permissionName))
            {
                permissions = permissions.Where(p => p.Name == permissionName);
            }
            return permissions.Where(p => !p.IsDeleted);
        }

        public Permission GetPermission(Guid permissionId)
        {
            if (Permissions.ContainsKey(permissionId))
            {
                return Permissions[permissionId];
            }
            throw new PermissionNotFoundException();
        }

        public Permission AddPermission(Permission permission)
        {
            permission.Id = Guid.NewGuid();
            permission.CreatedDateTimeUtc = DateTime.UtcNow;
            Permissions.TryAdd(permission.Id, permission);
            return permission;
        }

        public void DeletePermission(Permission permission)
        {
            permission.IsDeleted = true;
            permission.ModifiedDateTimeUtc = DateTime.UtcNow;
            UpdatePermission(permission);
        }

        public void UpdatePermission(Permission permission)
        {
            permission.ModifiedDateTimeUtc = DateTime.UtcNow;
        }
    }
}
