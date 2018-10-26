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
            mockGroupStore.Setup(groupStore => groupStore.Get(It.IsAny<GroupIdentifier>()))
                .Returns((GroupIdentifier groupIdentifier) =>
                {
                    if (groups.Any(g => g.Name == groupIdentifier.GroupName && g.TenantId == groupIdentifier.TenantId && g.IdentityProvider == groupIdentifier.IdentityProvider))
                    {
                        return Task.FromResult(groups.First(g => 
                            string.Equals(g.Name, groupIdentifier.GroupName, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(g.TenantId, groupIdentifier.TenantId, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(g.IdentityProvider, groupIdentifier.IdentityProvider, StringComparison.OrdinalIgnoreCase)));
                    }
                    throw new NotFoundException<Group>();
                });

            mockGroupStore.Setup(groupStore => groupStore.Get(It.IsAny<IEnumerable<GroupIdentifier>>(), It.IsAny<bool>()))
                .Returns((IEnumerable<GroupIdentifier> groupIdentifiers, bool ignoreMissingGroups) =>
            {
                var filteredEntities = new List<Group>();
                foreach (var identifier in groupIdentifiers)
                {
                    var entity = groups.FirstOrDefault(g =>
                        g.Name == identifier.GroupName
                        && g.TenantId == identifier.TenantId
                        && g.IdentityProvider == identifier.IdentityProvider);

                    if (entity != null)
                    {
                        filteredEntities.Add(entity);
                    }
                }

                return Task.FromResult(filteredEntities.AsEnumerable());
            });

            mockGroupStore.Setup(groupStore => groupStore.GetGroupsByIdentifiers(It.IsAny<IEnumerable<string>>()))
                .Returns((IEnumerable<string> groupNames) =>
                {
                    var groupNameList = groupNames.ToList();
                    return Task.FromResult(groups.Where(
                        g => groupNameList.Contains(g.Name, StringComparer.OrdinalIgnoreCase)
                             || groupNameList.Contains(g.ExternalIdentifier, StringComparer.OrdinalIgnoreCase)));
                });

            return mockGroupStore;
        }

        public static Mock<IGroupStore> SetupGroupExists(this Mock<IGroupStore> mockGroupStore, List<Group> groups)
        {
            mockGroupStore.Setup(groupStore => groupStore.Exists(It.IsAny<GroupIdentifier>()))
                .Returns((GroupIdentifier groupIdentifier) =>
                {
                    return Task.FromResult(groups.Any(
                        g => string.Equals(g.Name, groupIdentifier.GroupName, StringComparison.OrdinalIgnoreCase)
                             && string.Equals(g.TenantId, groupIdentifier.TenantId, StringComparison.OrdinalIgnoreCase)
                             && string.Equals(g.IdentityProvider, groupIdentifier.IdentityProvider, StringComparison.OrdinalIgnoreCase)));
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