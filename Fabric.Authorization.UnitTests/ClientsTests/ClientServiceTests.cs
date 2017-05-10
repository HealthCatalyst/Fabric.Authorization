using System;
using System.Collections.Generic;
using Fabric.Authorization.Domain;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.UnitTests.Mocks;
using Moq;
using Xunit;

namespace Fabric.Authorization.UnitTests.ClientsTests
{
    public class ClientServiceTests
    {
        private Client _testClient = new Client
        {
            Id = "sampleapplication",
            TopLevelResource = new Resource
            {
                Id = Guid.NewGuid(),
                Name = "sampleapplication",
                Resources = new List<Resource>
                {
                    new Resource
                    {
                        Id = Guid.NewGuid(),
                        Name = "ehr1",
                        Resources = new List<Resource>
                        {
                            new Resource
                            {
                                Id = Guid.NewGuid(),
                                Name = "patient",
                            },
                            new Resource
                            {
                                Id = Guid.NewGuid(),
                                Name = "diagnoses"
                            }
                        }
                    },
                    new Resource
                    {
                        Id = Guid.NewGuid(),
                        Name = "ehr2",
                        Resources = new List<Resource>
                        {
                            new Resource
                            {
                                Id = Guid.NewGuid(),
                                Name = "patient"
                            },
                            new Resource
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
        public void ClientService_DoesClientOwnResource_TopLevelMatch(string clientId, string grain, string resource, bool expectedResult)
        {
            var mockClientStore = new Mock<IClientStore>()
                .SetupGetClient(new List<Client> { _testClient })
                .Create();

            var clientService = new ClientService(mockClientStore);
            var ownsRequestedResource =
                clientService.DoesClientOwnResource(clientId, grain, resource);
            Assert.Equal(expectedResult, ownsRequestedResource);
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
