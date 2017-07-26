using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores
{
    public class InMemoryRoleStore : IRoleStore
    {
        private readonly ConcurrentDictionary<Guid, Role> Roles = new ConcurrentDictionary<Guid, Role>();

        public Task<IEnumerable<Role>> GetRoles(string grain = null, string securableItem = null, string roleName = null)
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
            return Task.FromResult(roles.Where(r => !r.IsDeleted));
        }

        public async Task<Role> Get(Guid roleId)
        {
            if (await Exists(roleId))
            {
                return Roles[roleId];
            }
            throw new NotFoundException<Role>($"The specified role with id: {roleId} was not found.");
        }

        public Task<Role> Add(Role role)
        {
            role.Track(creation: true);
            role.Id = Guid.NewGuid();
            Roles.TryAdd(role.Id, role);
            return Task.FromResult(role);
        }

        public async Task Delete(Role role)
        {
            role.IsDeleted = true;
            await Update(role);
        }

        public async Task Update(Role role)
        {
            role.Track();

            if (await this.Exists(role.Id))
            {
                if (!Roles.TryUpdate(role.Id, role, await this.Get(role.Id)))
                {
                    throw new CouldNotCompleteOperationException();
                }
            }
            else
            {
                throw new NotFoundException<Role>(role, role.Id.ToString());
            }
        }

        public Task<IEnumerable<Role>> GetAll() => this.GetRoles();

        public Task<bool> Exists(Guid id) => Task.FromResult(Roles.ContainsKey(id));
        
    }
}