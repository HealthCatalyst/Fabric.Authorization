using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores
{
    public interface IRoleStore : IGenericStore<Guid, Role>
    {
        Task<IEnumerable<Role>> GetRoles(string grain, string securableItem = null, string roleName = null);
    }
}