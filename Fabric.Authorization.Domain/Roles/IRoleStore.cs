using System;
using System.Collections.Generic;
using System.Text;

namespace Fabric.Authorization.Domain.Roles
{
    public interface IRoleStore
    {
        IEnumerable<Role> GetRoles(string grain = null, string resource = null, string roleName = null);
        Role GetRole(Guid roleId);

        void DeleteRole(Role role);

        void AddRole(Role role);

        void UpdateRole(Role role);

    }
}
