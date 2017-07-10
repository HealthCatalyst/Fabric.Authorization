using System;
using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores
{
    public interface IRoleStore : IGenericStore<Guid, Role>
    {
        IEnumerable<Role> GetRoles(string grain = null, string securableItem = null, string roleName = null);
    }
}
