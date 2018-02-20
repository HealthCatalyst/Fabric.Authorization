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
    public static class SecurableItemStoreMockExtensions
    {
        public static Mock<ISecurableItemStore> SetupGetSecurableItem(this Mock<ISecurableItemStore> mockSecurableItemStore, List<SecurableItem> securableItems)
        {
            mockSecurableItemStore.Setup(securableItemStore => securableItemStore.Get(It.IsAny<string>()))
                .Returns((string securableItemName) =>
                {
                    if (securableItems.Any(s => s.Name == securableItemName))
                    {
                        return Task.FromResult(securableItems.First(s => s.Name == securableItemName));
                    }
                    throw new NotFoundException<SecurableItem>();
                });

            mockSecurableItemStore.Setup(securableItemStore => securableItemStore.Get(It.IsAny<Guid>()))
                .Returns((Guid id) =>
                {
                    if (securableItems.Any(s => s.Id == id))
                    {
                        return Task.FromResult(securableItems.First(s => s.Id == id));
                    }
                    throw new NotFoundException<SecurableItem>();
                });

            return mockSecurableItemStore;
        }

        public static ISecurableItemStore Create(this Mock<ISecurableItemStore> mockSecurableItemStore)
        {
            return mockSecurableItemStore.Object;
        }
    }
}
