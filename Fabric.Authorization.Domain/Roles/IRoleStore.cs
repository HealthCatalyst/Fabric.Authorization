using System;
using System.Collections.Generic;
using System.Text;

namespace Fabric.Authorization.Domain.Roles
{
    public interface IRoleStore
    {
        IEnumerable<Role> GetRoles(string grain = null, string resource = null);
        Role GetRole(int roleId);

        void AddRole(Role role);

        void UpdateRole(Role role);

    }
}
