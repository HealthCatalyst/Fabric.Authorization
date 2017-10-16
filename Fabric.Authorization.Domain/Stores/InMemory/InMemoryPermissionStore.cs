using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores.InMemory
{
    public class InMemoryPermissionStore : InMemoryGenericStore<Permission>, IPermissionStore
    {

        public override async Task<Permission> Add(Permission model)
        {
            model.Id = Guid.NewGuid();
            return await base.Add(model);
        }

        public async Task<bool> Exists(Guid id) => await this.Exists(id.ToString());

        public async Task<Permission> Get(Guid id) => await this.Get(id.ToString());

        public Task<IEnumerable<Permission>> GetPermissions(string grain = null, string securableItem = null, string permissionName = null)
        {
            var permissions = Dictionary.Select(kvp => kvp.Value);
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


        private readonly ConcurrentDictionary<string, GranularPermission> granularPermissions = new ConcurrentDictionary<string, GranularPermission>();

        public Task AddOrUpdateGranularPermission(GranularPermission granularPermission)
        {
            var success = granularPermissions.TryAdd(granularPermission.Id, granularPermission);
            if (!success)
            {
                granularPermissions.TryUpdate(granularPermission.Id, granularPermission, granularPermissions[granularPermission.Id]);
            }
            return Task.CompletedTask;
        }

        public Task<GranularPermission> GetGranularPermission(string userId)
        {
            granularPermissions.TryGetValue(userId, out GranularPermission value);

            if (value == null)
            {
                throw new NotFoundException<GranularPermission>(userId);
            }

            return Task.FromResult(value);
        }
    }
}