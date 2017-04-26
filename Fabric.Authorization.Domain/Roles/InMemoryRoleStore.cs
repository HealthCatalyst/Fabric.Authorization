using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Fabric.Authorization.Domain.Roles
{
    public class InMemoryRoleStore : IRoleStore
    {
        private static readonly ConcurrentDictionary<int, Role> Roles = new ConcurrentDictionary<int, Role>();

        static InMemoryRoleStore()
        {
            var role1 = new Role
            {
                Id = 1,
                Grain = "app",
                Resource = "patientsafety",
                Name = "Admin"
            };

            var role2 = new Role
            {
                Id = 2,
                Grain = "app",
                Resource = "sourcemartdesigner",
                Name = "Admin",
            };

            var role3 = new Role
            {
                Id = 3,
                Grain = "app",
                Resource = "sourcemartdesigner",
                Name = "Contributor",
            };
            Roles.TryAdd(role1.Id, role1);
            Roles.TryAdd(role2.Id, role2);
            Roles.TryAdd(role3.Id, role3);
        }

        public IEnumerable<Role> GetRoles(string grain = null, string resource = null)
        {
            return Roles.Where(kvp => kvp.Value.Grain == grain && kvp.Value.Resource == resource).Select(kvp => kvp.Value);
        }

        public Role GetRole(int roleId)
        {
            return Roles.ContainsKey(roleId) ? Roles[roleId] : null;
        }

        public void AddRole(Role role)
        {
            Roles.TryAdd(role.Id, role);
        }

        public void UpdateRole(Role role)
        {
            //do nothing since this is an in memory store
        }
    }
}
