using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                        return groups.First(g => g.Name == groupName);
                    }
                    throw new NotFoundException<Group>();
                });
            return mockGroupStore;
        }

        public static Mock<IGroupStore> SetupGroupExists(this Mock<IGroupStore> mockGroupStore, List<Group> groups)
        {
            mockGroupStore.Setup(groupStore => groupStore.Exists(It.IsAny<string>()))
                .Returns((string groupName) =>
                {
                    return groups.Any(g => g.Name == groupName);
                });
            return mockGroupStore;
        }
        public static Mock<IGroupStore> SetupAddGroup(this Mock<IGroupStore> mockGroupStore)
        {
            mockGroupStore.Setup(GroupStore => GroupStore.Add(It.IsAny<Group>()))
                .Returns((Group g) =>
                {
                    g.Id = Guid.NewGuid().ToString();
                    return g;
                });
            return mockGroupStore;
        }
    }
}
