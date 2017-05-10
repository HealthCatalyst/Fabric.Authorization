using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores
{
    public class InMemoryRoleStore : IRoleStore
    {
        private static readonly ConcurrentDictionary<Guid, Role> Roles = new ConcurrentDictionary<Guid, Role>();
        
        public IEnumerable<Role> GetRoles(string grain = null, string resource = null, string roleName = null)
        {
            var roles = Roles.Select(kvp => kvp.Value);
            if (!string.IsNullOrEmpty(grain))
            {
                roles = roles.Where(r => r.Grain == grain);
            }
            if (!string.IsNullOrEmpty(resource))
            {
                roles = roles.Where(r => r.Resource == resource);
            }
            if (!string.IsNullOrEmpty(roleName))
            {
                roles = roles.Where(r => r.Name == roleName);
            }
            return roles.Where(r => !r.IsDeleted);
        }

        public Role GetRole(Guid roleId)
        {
            if (Roles.ContainsKey(roleId))
            {
                return Roles[roleId];
            }
            throw new RoleNotFoundException();
        }

        public void AddRole(Role role)
        {
            role.Id = Guid.NewGuid();
            Roles.TryAdd(role.Id, role);
        }

        public void DeleteRole(Role role)
        {
            role.IsDeleted = true;
            UpdateRole(role);
        }

        public void UpdateRole(Role role)
        {
            //do nothing since this is an in memory store
        }
    }
}
