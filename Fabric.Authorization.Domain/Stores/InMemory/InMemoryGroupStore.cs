using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores
{
    public class InMemoryGroupStore : IGroupStore
    {
        private readonly ConcurrentDictionary<string, Group> Groups = new ConcurrentDictionary<string, Group>();

        [Obsolete]
        public InMemoryGroupStore()
        {
            var group1 = new Group
            {
                Id = Guid.NewGuid().ToString(),
                Name = @"FABRIC\Health Catalyst Viewer",
            };

            var group2 = new Group
            {
                Id = Guid.NewGuid().ToString(),
                Name = @"FABRIC\Health Catalyst Editor",
            };

            Groups.TryAdd(group1.Name, group1);
            Groups.TryAdd(group2.Name, group2);
        }

        public async Task<Group> Get(string name)
        {
            if (await Exists(name) && !Groups[name].IsDeleted)
            {
                return Groups[name];
            }

            throw new NotFoundException<Group>(name);
        }

        public async Task<Group> Add(Group group)
        {
            group.Track(creation: true);

            if (await Exists(group.Name))
            {
                throw new AlreadyExistsException<Group>(group, group.Name);
            }

            Groups.TryAdd(group.Name, group);
            return group;
        }

        public async Task Delete(Group group)
        {
            group.IsDeleted = true;
            await Update(group);
        }

        public async Task Update(Group group)
        {
            group.Track();

            if (await this.Exists(group.Name))
            {
                if (!Groups.TryUpdate(group.Name, group, Groups[group.Name]))
                {
                    throw new CouldNotCompleteOperationException();
                }
            }
            else
            {
                throw new NotFoundException<Group>(group, group.Name.ToString());
            }
        }

        public Task<bool> Exists(string name) => Task.FromResult(Groups.ContainsKey(name));

        public Task<IEnumerable<Group>> GetAll() => Task.FromResult(Groups.Values.Where(g => !g.IsDeleted));
    }
}