using System;
using System.Collections.Concurrent;
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

        public Group GetGroup(string groupName)
        {
            if (GroupExists(groupName))
            {
                return Groups[groupName];
            }
            throw new GroupNotFoundException(groupName);
        }

        public Group AddGroup(Group group)
        {
            if (GroupExists(group.Name))
            {
                throw new GroupAlreadyExistsException(group.Name);
            }

            group.Id = Guid.NewGuid().ToString();
            Groups.TryAdd(group.Name, group);
            return group;
        }

        public Group DeleteGroup(string groupName)
        {
            if (GroupExists(groupName))
            {
                if (!Groups.TryRemove(groupName, out var removedGroup))
                {
                    //TODO: Manage exceptions
                    throw new InvalidOperationException($"Failed to delete '{groupName}'");
                }

                return removedGroup;
            }

            throw new GroupNotFoundException(groupName);
        }

        public bool GroupExists(string groupName) => Groups.ContainsKey(groupName);
    }
}