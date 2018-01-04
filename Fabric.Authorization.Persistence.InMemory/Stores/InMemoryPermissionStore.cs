using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;

namespace Fabric.Authorization.Persistence.InMemory.Stores
{
    public class InMemoryPermissionStore : InMemoryFormattableIdentifierStore<Permission>, IPermissionStore
    {
        private readonly ConcurrentDictionary<string, GranularPermission> _granularPermissions =
            new ConcurrentDictionary<string, GranularPermission>();

        public InMemoryPermissionStore(IIdentifierFormatter identifierFormatter) : base(identifierFormatter)
        {
            
        }

        public override async Task<Permission> Add(Permission model)
        {
            model.Id = Guid.NewGuid();
            return await base.Add(model);
        }

        public async Task<bool> Exists(Guid id)
        {
            return await Exists(id.ToString());
        }

        public async Task<Permission> Get(Guid id)
        {
            return await Get(id.ToString());
        }

        public Task<IEnumerable<Permission>> GetPermissions(string grain = null, string securableItem = null,
            string permissionName = null)
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

        public Task AddOrUpdateGranularPermission(GranularPermission granularPermission)
        {
            var formattedId = FormatId(granularPermission.Id);

            var success = _granularPermissions.TryAdd(formattedId, granularPermission);
            if (!success)
            {
                granularPermission.Track(false);
                _granularPermissions.TryUpdate(formattedId, granularPermission,
                    _granularPermissions[formattedId]);
            }
            else
            {
                granularPermission.Track();
            }
            return Task.CompletedTask;
        }

        public Task<GranularPermission> GetGranularPermission(string userId)
        {
            var formattedId = FormatId(userId);
            _granularPermissions.TryGetValue(formattedId, out GranularPermission value);

            if (value == null)
            {
                throw new NotFoundException<GranularPermission>(userId);
            }

            return Task.FromResult(value);
        }
    }
}