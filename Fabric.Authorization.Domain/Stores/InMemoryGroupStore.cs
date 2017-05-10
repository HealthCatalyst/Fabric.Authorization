using System;
using System.Collections.Concurrent;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores
{
    public class InMemoryGroupStore : IGroupStore
    {
        public static ConcurrentDictionary<string, Group> Groups = new ConcurrentDictionary<string, Group>();

        static InMemoryGroupStore()
        {
            var group1 = new Group
            {
                Id = Guid.NewGuid().ToString(),
                Name = "HC PatientSafety Admin",
            };

            var group2 = new Group
            {
                Id = Guid.NewGuid().ToString(),
                Name = "HC SourceMartDesigner Admin",
            };

            Groups.TryAdd(group1.Name, group1);
            Groups.TryAdd(group2.Name, group2);
        }

        public Group GetGroup(string groupName)
        {
            if (Groups.ContainsKey(groupName))
            {
                return Groups[groupName];
            }
            throw new GroupNotFoundException();
        }
    }
}
