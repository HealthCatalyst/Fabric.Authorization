using System;
using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores
{
    public interface IPermissionStore : IGenericStore<Guid, Permission>
    {
        IEnumerable<Permission> GetPermissions(string grain = null, string securableItem = null, string permissionName = null);
    }
}
