using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores
{
    public interface IPermissionStore : IGenericStore<Guid, Permission>
    {
        Task<IEnumerable<Permission>> GetPermissions(string grain, string securableItem = null, string permissionName = null);
    }
}