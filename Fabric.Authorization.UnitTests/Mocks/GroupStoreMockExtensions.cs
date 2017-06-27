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
        public static Mock<IGroupStore> SetupGetGroup(this Mock<IGroupStore> mockGroupStore, List<Group> groups)
        {
            mockGroupStore.Setup(groupStore => groupStore.GetGroup(It.IsAny<string>()))
                .Returns((string groupName) =>
                {
                    if (groups.Any(g => g.Name == groupName))
                    {
                        return groups.First(g => g.Name == groupName);
                    }
                    throw new GroupNotFoundException();
                });
            return mockGroupStore;
        }

        public static Mock<IGroupStore> SetupGroupExists(this Mock<IGroupStore> mockGroupStore, List<Group> groups)
        {
            mockGroupStore.Setup(groupStore => groupStore.GroupExists(It.IsAny<string>()))
                .Returns((string groupName) =>
                {
                    return groups.Any(g => g.Name == groupName);
                });
            return mockGroupStore;
        }
    }
}
