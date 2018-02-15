using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores;
using Moq;
using Xunit;

namespace Fabric.Authorization.UnitTests.SecurableItems
{
    public class SecurableItemServiceTests
    {
        [Fact]
        public void IsSecurableItemChildOfGrain_Deep_ReturnsTrue()
        {
            var securableItemService = new SecurableItemService(new Mock<IClientStore>().Object);
            var deepGrain = GetGrainWithDeepGraph();
            Assert.True(securableItemService.IsSecurableItemChildOfGrain(deepGrain, "level_one_a"));
            Assert.True(securableItemService.IsSecurableItemChildOfGrain(deepGrain, "level_one_b"));
            Assert.True(securableItemService.IsSecurableItemChildOfGrain(deepGrain, "level_two"));
            Assert.True(securableItemService.IsSecurableItemChildOfGrain(deepGrain, "level_three_a"));
            Assert.True(securableItemService.IsSecurableItemChildOfGrain(deepGrain, "level_three_b"));
        }

        [Fact]
        public void IsSecurableItemChildOfGrain_Deep_ReturnsFalse()
        {
            var securableItemService = new SecurableItemService(new Mock<IClientStore>().Object);
            var deepGrain = GetGrainWithDeepGraph();
            Assert.False(securableItemService.IsSecurableItemChildOfGrain(deepGrain, "level_four"));
        }

        private Grain GetGrainWithDeepGraph()
        {
            var grain = new Grain
            {
                Name = "shared",
                IsShared = true,
                SecurableItems = new List<SecurableItem>
                {
                    new SecurableItem
                    {
                        Name = "level_one_a",
                        SecurableItems = new List<SecurableItem>
                        {
                            new SecurableItem
                            {
                                Name = "level_two",
                                SecurableItems = new List<SecurableItem>
                                {
                                    new SecurableItem
                                    {
                                        Name = "level_three_a"
                                    },
                                    new SecurableItem
                                    {
                                        Name = "level_three_b"
                                    }
                                }
                            }
                        }
                    },
                    new SecurableItem
                    {
                        Name = "level_one_b"
                    }
                }
            };

            return grain;

        }
    }
}
