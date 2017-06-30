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

        public Group Get(string id)
        {
            if (Exists(id))
            {
                return Groups[id];
            }

            throw new GroupNotFoundException(id);
        }

        public Group Add(Group group)
        {
            group.Track(creation: true);

            if (Exists(group.Id))
            {
                throw new GroupAlreadyExistsException(group.Id);
            }

            Groups.TryAdd(group.Id, group);
            return group;
        }

        public void Delete(Group group)
        {
            group.IsDeleted = true;
            UpdateGroup(group);
        }

        public void UpdateGroup(Group group)
        {
            group.Track();

            if (this.Exists(group.Id))
            {
                if (!Groups.TryUpdate(group.Id, group, this.Get(group.Id)))
                {
                    throw new CouldNotCompleteOperationException();
                }
            }
            else
            {
                throw new GroupNotFoundException(group.Id.ToString());
            }
        }

        public bool Exists(string id) => Groups.ContainsKey(id);

        public IEnumerable<Group> GetAll() => Groups.Values;
    }
}