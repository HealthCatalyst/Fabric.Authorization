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
        public IEnumerable<Permission> GetPermissions(string grain = null, string securableItem = null, string permissionName = null)
        {
            var permissions = Permissions.Select(kvp => kvp.Value);
            if (!string.IsNullOrEmpty(grain))
            {
                permissions = permissions.Where(p => p.Grain == grain);
            }
            if (!string.IsNullOrEmpty(securableItem))
            {
                permissions = permissions.Where(p => p.SecurableItem == securableItem);
            }
            if (!string.IsNullOrEmpty(permissionName))
            {
                permissions = permissions.Where(p => p.Name == permissionName);
            }
            return permissions.Where(p => !p.IsDeleted);
        }

        public Permission Get(Guid permissionId)
        {
            if (Permissions.ContainsKey(permissionId))
            {
                return Permissions[permissionId];
            }
            throw new PermissionNotFoundException();
        }

        public Permission Add(Permission permission)
        {
            permission.Track(creation: true);

            permission.Id = Guid.NewGuid();
            Permissions.TryAdd(permission.Id, permission);
            return permission;
        }

        public void Delete(Permission permission)
        {
            permission.IsDeleted = true;
            UpdatePermission(permission);
        }

        public void UpdatePermission(Permission permission)
        {

            if (this.Exists(permission.Id))
            {
                if (!Permissions.TryUpdate(permission.Id, permission, this.Get(permission.Id)))
                {
                    throw new CouldNotCompleteOperationException();
                }
            }
            else
            {
                throw new PermissionNotFoundException(permission.Id.ToString());
            }
        }

        public IEnumerable<Permission> GetAll() => this.GetPermissions();
        public bool Exists(Guid id) => false;
    }
}
