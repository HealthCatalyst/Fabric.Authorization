using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;
using Moq;

namespace Fabric.Authorization.UnitTests.Mocks
{
    public static class GroupStoreMockExtensions
    {
        public static Mock<IGroupStore> SetupGetGroups(this Mock<IGroupStore> mockGroupStore, List<Group> groups)
        {
            mockGroupStore.Setup(groupStore => groupStore.Get(It.IsAny<string>()))
                .Returns((string groupName) =>
                {
                    if (groups.Any(g => g.Name == groupName))
                    {
                        return Task.FromResult(groups.First(g => string.Equals(g.Name, groupName, StringComparison.OrdinalIgnoreCase)));
                    }
                    throw new NotFoundException<Group>();
                });

            mockGroupStore.Setup(groupStore => groupStore.Get(It.IsAny<IEnumerable<string>>(), It.IsAny<bool>()))
                .Returns((IEnumerable<string> groupNames, bool ignoreMissingGroups) =>
            {
                return Task.FromResult(groups.Where(
                    g => groupNames.Contains(g.Name, StringComparer.OrdinalIgnoreCase)));
            });

            return mockGroupStore;
        }

        public static Mock<IGroupStore> SetupGroupExists(this Mock<IGroupStore> mockGroupStore, List<Group> groups)
        {
            mockGroupStore.Setup(groupStore => groupStore.Exists(It.IsAny<string>()))
                .Returns((string groupName) =>
                    {
                        return Task.FromResult(groups.Any(
                            g => string.Equals(g.Name, groupName, StringComparison.OrdinalIgnoreCase)));
                    });
            return mockGroupStore;
        }

        public static Mock<IGroupStore> SetupAddGroup(this Mock<IGroupStore> mockGroupStore)
        {
            mockGroupStore.Setup(groupStore => groupStore.Add(It.IsAny<Group>()))
                .Returns((Group g) =>
                {
                    g.Id = Guid.NewGuid();
                    return Task.FromResult(g);
                });
            return mockGroupStore;
        }

        public static IGroupStore Create(this Mock<IGroupStore> mockGroupStore)
        {
            return mockGroupStore.Object;
        }
    }
}