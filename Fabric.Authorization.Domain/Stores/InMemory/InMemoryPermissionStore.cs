using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores
{
    public class InMemoryPermissionStore : IPermissionStore
    {
        private readonly ConcurrentDictionary<Guid, Permission> Permissions = new ConcurrentDictionary<Guid, Permission>();

        public Task<IEnumerable<Permission>> GetPermissions(string grain = null, string securableItem = null, string permissionName = null)
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
            return Task.FromResult(permissions.Where(p => !p.IsDeleted));
        }

        public Task<Permission> Get(Guid permissionId)
        {
            if (Permissions.ContainsKey(permissionId) &&  !Permissions[permissionId].IsDeleted)
            {
                return Task.FromResult(Permissions[permissionId]);
            }
            throw new NotFoundException<Permission>(permissionId.ToString());
        }

        public Task<Permission> Add(Permission permission)
        {
            permission.Track(creation: true);

            permission.Id = Guid.NewGuid();
            Permissions.TryAdd(permission.Id, permission);
            return Task.FromResult(permission);
        }

        public async Task Delete(Permission permission)
        {
            permission.IsDeleted = true;
            await Update(permission);
        }

        public async Task Update(Permission permission)
        {
            if (await this.Exists(permission.Id))
            {
                if (!Permissions.TryUpdate(permission.Id, permission, await this.Get(permission.Id)))
                {
                    throw new CouldNotCompleteOperationException();
                }
            }
            else
            {
                throw new NotFoundException<Permission>(permission.Id.ToString());
            }
        }

        public Task<IEnumerable<Permission>> GetAll() => this.GetPermissions();

        public Task<bool> Exists(Guid id) => Task.FromResult(Permissions.ContainsKey(id));
    }
}