using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;
using Moq;

namespace Fabric.Authorization.UnitTests.Mocks
{
    public static class MockGrainStore
    {
        public static Mock<IGrainStore> SetupGetGrain(this Mock<IGrainStore> mockGrainStore, List<Grain> grains)
        {
            mockGrainStore.Setup(grainStore => grainStore.Get(It.IsAny<string>()))
                .Returns((string grainName) =>
                    Task.FromResult(grains.Single(g => g.Name == grainName)));

            return mockGrainStore;
        }

        public static Mock<IGrainStore> SetupGetSharedGrains(this Mock<IGrainStore> mockGrainStore, List<Grain> grains)
        {
            mockGrainStore.Setup(grainStore => grainStore.GetSharedGrains())
                .Returns((string grainName) =>
                    Task.FromResult(grains.Where(g => g.IsShared)));

            return mockGrainStore;
        }

        public static Mock<IGrainStore> SetupGetAllGrain(this Mock<IGrainStore> mockGrainStore, IEnumerable<Grain> grains)
        {
            mockGrainStore.Setup(grainStore => grainStore.GetAll())
                .Returns(Task.FromResult(grains));

            return mockGrainStore;
        }
    }
}