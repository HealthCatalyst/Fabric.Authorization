using System;
using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores
{
    public interface IRoleStore
    {
        IEnumerable<Role> GetRoles(string grain = null, string securableItem = null, string roleName = null);
        Role GetRole(Guid roleId);

        void DeleteRole(Role role);

        Role AddRole(Role role);

        void UpdateRole(Role role);

    }
}
