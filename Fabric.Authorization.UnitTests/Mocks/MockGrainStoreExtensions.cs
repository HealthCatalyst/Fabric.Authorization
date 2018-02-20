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
    public static class MockGrainStoreExtensions
    {
        public static Mock<IGrainStore> SetupMock(this Mock<IGrainStore> mockGrainStore, List<Grain> grains)
        {
            mockGrainStore.Setup(grainStore => grainStore.Get(It.IsAny<string>()))
                .Returns((string grainName) =>
                {
                    if (grains.Any(s => s.Name == grainName))
                    {
                        return Task.FromResult(grains.First(g => g.Name == grainName && !g.IsDeleted));
                    }
                    throw new NotFoundException<Grain>();
                });


            mockGrainStore.Setup(grainStore => grainStore.GetSharedGrains())
                .Returns(() =>
                {
                    return Task.FromResult(grains.Where(g => g.IsShared && !g.IsDeleted).AsEnumerable());
                });

            return mockGrainStore;
        }
    }
}