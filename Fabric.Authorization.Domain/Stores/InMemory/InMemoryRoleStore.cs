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
        
        public IEnumerable<Role> GetRoles(string grain = null, string securableItem = null, string roleName = null)
        {
            var roles = Roles.Select(kvp => kvp.Value);
            if (!string.IsNullOrEmpty(grain))
            {
                roles = roles.Where(r => r.Grain == grain);
            }
            if (!string.IsNullOrEmpty(securableItem))
            {
                roles = roles.Where(r => r.SecurableItem == securableItem);
            }
            if (!string.IsNullOrEmpty(roleName))
            {
                roles = roles.Where(r => r.Name == roleName);
            }
            return roles.Where(r => !r.IsDeleted);
        }

        public Role Get(Guid roleId)
        {
            if (Exists(roleId))
            {
                return Roles[roleId];
            }
            throw new NotFoundException<Role>($"The specified role with id: {roleId} was not found.");
        }

        public Role Add(Role role)
        {
            role.Track(creation: true);
            role.Id = Guid.NewGuid();
            Roles.TryAdd(role.Id, role);
            return role;
        }

        public void Delete(Role role)
        {
            role.IsDeleted = true;
            Update(role);
        }

        public void Update(Role role)
        {
            role.Track();

            if (this.Exists(role.Id))
            {
                if (!Roles.TryUpdate(role.Id, role, this.Get(role.Id)))
                {
                    throw new CouldNotCompleteOperationException();
                }
            }
            else
            {
                throw new NotFoundException<Role>(role, role.Id.ToString());
            }
        }

        public IEnumerable<Role> GetAll() => this.GetRoles();

        public bool Exists(Guid id) => Roles.ContainsKey(id);
    }
}
