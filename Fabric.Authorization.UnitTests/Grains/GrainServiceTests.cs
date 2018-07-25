using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.UnitTests.Mocks;
using Moq;
using Xunit;

namespace Fabric.Authorization.UnitTests.Grains
{
    public class GrainServiceTests
    {
        [Fact]
        public async Task IsGrainWithChildren_Deep_ReturnsGrainWithChildrenAsync()
        {
            // Arrange
            var mockGrainStore = new Mock<IGrainStore>()
                    .SetupGetAllGrain(GetGrainWithDeepGraph())
                    .Object;

            var subject = new GrainService(mockGrainStore);

            // Act
            var actualResult = await subject.GetAllGrains();

            // Assert
            var first = actualResult.First(g => g.Name == "shared");
            Assert.True(first.SecurableItems.Count == 2);
            Assert.True(first.IsSecurableItemChildOfGrain("level_one_a"));
            Assert.True(first.IsSecurableItemChildOfGrain("level_one_b"));
            Assert.True(first.IsSecurableItemChildOfGrain("level_two"));
            Assert.True(first.IsSecurableItemChildOfGrain("level_three_a"));
            Assert.True(first.IsSecurableItemChildOfGrain("level_three_b"));

            var second = actualResult.First(g => g.Name == "shared2");
            Assert.True(second.SecurableItems.Count == 2);
            Assert.True(second.IsSecurableItemChildOfGrain("level_one_a2"));
            Assert.True(second.IsSecurableItemChildOfGrain("level_one_b2"));
            Assert.True(second.IsSecurableItemChildOfGrain("level_two2"));
            Assert.True(second.IsSecurableItemChildOfGrain("level_three_a2"));
            Assert.True(second.IsSecurableItemChildOfGrain("level_three_b2"));
        }
        
        private static IEnumerable<Grain> GetGrainWithDeepGraph()
        {
            var grain1 = new Grain
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
            var grain2 = new Grain
            {
                Name = "shared2",
                IsShared = true,
                SecurableItems = new List<SecurableItem>
                {
                    new SecurableItem
                    {
                        Name = "level_one_a2",
                        SecurableItems = new List<SecurableItem>
                        {
                            new SecurableItem
                            {
                                Name = "level_two2",
                                SecurableItems = new List<SecurableItem>
                                {
                                    new SecurableItem
                                    {
                                        Name = "level_three_a2"
                                    },
                                    new SecurableItem
                                    {
                                        Name = "level_three_b2"
                                    }
                                }
                            }
                        }
                    },
                    new SecurableItem
                    {
                        Name = "level_one_b2"
                    }
                }
            };

            return new[] { grain1, grain2 };
        }
    }
}
