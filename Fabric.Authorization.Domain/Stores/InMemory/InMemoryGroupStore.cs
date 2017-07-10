using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores
{
    public class InMemoryGroupStore : IGroupStore
    {
        private static readonly ConcurrentDictionary<string, Group> Groups = new ConcurrentDictionary<string, Group>();

        [Obsolete]
        static InMemoryGroupStore()
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

        public Group Get(string name)
        {
            if (Exists(name) && !Groups[name].IsDeleted)
            {
                return Groups[name];
            }

            throw new NotFoundException<Group>(name);
        }

        public Group Add(Group group)
        {
            group.Track(creation: true);

            if (Exists(group.Name))
            {
                throw new AlreadyExistsException<Group>(group, group.Name);
            }

            Groups.TryAdd(group.Name, group);
            return group;
        }

        public void Delete(Group group)
        {
            group.IsDeleted = true;
            Update(group);
        }

        public void Update(Group group)
        {
            group.Track();

            if (this.Exists(group.Name))
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

        public bool Exists(string name) => Groups.ContainsKey(name);

        public IEnumerable<Group> GetAll() => Groups.Values;
    }
}