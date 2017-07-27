using System;
using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Domain.Stores.Services;
using Fabric.Authorization.UnitTests.Mocks;
using Moq;
using Xunit;

namespace Fabric.Authorization.UnitTests.Clients
{
    public class ClientServiceTests
    {
        private readonly Client _testClient = new Client
        {
            Id = "sampleapplication",
            TopLevelSecurableItem = new SecurableItem
            {
                Id = Guid.NewGuid(),
                Name = "sampleapplication",
                SecurableItems = new List<SecurableItem>
                {
                    new SecurableItem
                    {
                        Id = Guid.NewGuid(),
                        Name = "ehr1",
                        SecurableItems = new List<SecurableItem>
                        {
                            new SecurableItem
                            {
                                Id = Guid.NewGuid(),
                                Name = "patient",
                            },
                            new SecurableItem
                            {
                                Id = Guid.NewGuid(),
                                Name = "diagnoses"
                            }
                        }
                    },
                    new SecurableItem
                    {
                        Id = Guid.NewGuid(),
                        Name = "ehr2",
                        SecurableItems = new List<SecurableItem>
                        {
                            new SecurableItem
                            {
                                Id = Guid.NewGuid(),
                                Name = "patient"
                            },
                            new SecurableItem
                            {
                                Id = Guid.NewGuid(),
                                Name = "observations"
                            }
                        }
                    }
                }
            }
        };

        [Theory, MemberData(nameof(RequestData))]
        public void ClientService_DoesClientOwnItem_TopLevelMatch(string clientId, string grain, string securableItem, bool expectedResult)
        {
            var mockClientStore = new Mock<IClientStore>()
                .SetupGetClient(new List<Client> { _testClient })
                .Create();

            var clientService = new ClientService(mockClientStore);
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
