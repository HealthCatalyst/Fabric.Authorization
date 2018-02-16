using System;
using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.UnitTests.Mocks;
using Moq;
using Xunit;

namespace Fabric.Authorization.UnitTests.Clients
{
    public class ClientServiceTests
    {
        private readonly List<SecurableItem> _securableItems;
        private const string ClientId = "sampleapplication";
        private readonly Client _testClient = new Client
        {
            Id = ClientId
        };

        private readonly SecurableItem _topLevelSecurableItem = new SecurableItem
        {
            Id = Guid.NewGuid(),
            Name = ClientId,
            SecurableItems = new List<SecurableItem>
            {
                new SecurableItem
                {
                    Id = Guid.NewGuid(),
                    Name = "ehr1",
                    Grain = "ehr1",
                    ClientOwner = ClientId,
                    SecurableItems = new List<SecurableItem>
                    {
                        new SecurableItem
                        {
                            Id = Guid.NewGuid(),
                            Name = "patient",
                            Grain = "ehr1",
                            ClientOwner = ClientId
                        },
                        new SecurableItem
                        {
                            Id = Guid.NewGuid(),
                            Name = "diagnoses",
                            Grain = "ehr1",
                            ClientOwner = ClientId
                        }
                    }
                },
                new SecurableItem
                {
                    Id = Guid.NewGuid(),
                    Name = "ehr2",
                    Grain = "ehr2",
                    ClientOwner = ClientId,
                    SecurableItems = new List<SecurableItem>
                    {
                        new SecurableItem
                        {
                            Id = Guid.NewGuid(),
                            Name = "patient",
                            Grain = "ehr2",
                            ClientOwner = ClientId
                        },
                        new SecurableItem
                        {
                            Id = Guid.NewGuid(),
                            Name = "observations",
                            Grain = "ehr2"
                        }
                    }
                }
            }
        };

        public ClientServiceTests()
        {
            _testClient.TopLevelSecurableItem = _topLevelSecurableItem;
            _securableItems = new List<SecurableItem>{ _topLevelSecurableItem };
            InitializeSecurableItems(_topLevelSecurableItem);
        }

        private void InitializeSecurableItems(SecurableItem topLevelSecurableItem)
        {
            foreach (var securableItem in topLevelSecurableItem.SecurableItems)
            {
                _securableItems.Add(securableItem);
                InitializeSecurableItems(securableItem);
            }
        }

        [Theory, MemberData(nameof(RequestData))]
        public void ClientService_DoesClientOwnItem_TopLevelMatch(string clientId, string grain, string securableItem, bool expectedResult)
        {
            var mockClientStore = new Mock<IClientStore>()
                .SetupGetClient(new List<Client> { _testClient })
                .Create();

            var mockSecurableItemStore = new Mock<ISecurableItemStore>()
                .SetupGetSecurableItem(_securableItems)
                .Create();


            var clientService = new ClientService(mockClientStore, mockSecurableItemStore);
            var ownsRequestedItem =
                clientService.DoesClientOwnItem(clientId, grain, securableItem).Result;
            Assert.Equal(expectedResult, ownsRequestedItem);
        }

        public static IEnumerable<object[]> RequestData => new[]
        {
            new object[] { "sampleapplication", "app", "sampleapplication", true},
            new object[] { "sampleapplication", "ehr2", "patient", true},
            new object[] { "sampleapplication", "sampleapplication", "ehr1", true},
            new object[] { "sampleapplication", "sampleapplication", "ehr2", true},
            new object[] { "sampleapplication", "ehr1", "diagnoses", true},
            new object[] { "sampleapplication", "ehr1", "patient", true},
            new object[] { "sampleapplication", "ehr2", "observations", true},
            new object[] { "sampleapplication", "ehr1", "observations", false}
        };
    }
}
