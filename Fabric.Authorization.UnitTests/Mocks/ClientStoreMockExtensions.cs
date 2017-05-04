using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fabric.Authorization.Domain;
using Fabric.Authorization.Domain.Clients;
using Moq;

namespace Fabric.Authorization.UnitTests.Mocks
{
    public static class ClientStoreMockExtensions
    {
        public static Mock<IClientStore> SetupGetClient(this Mock<IClientStore> mockClientStore, List<Client> clients)
        {
            mockClientStore.Setup(clientStore => clientStore.GetClient(It.IsAny<string>()))
                .Returns((string clientId) => clients.First(c => c.Id == clientId));
            return mockClientStore;
        }

        public static IClientStore Create(this Mock<IClientStore> mockClientStore)
        {
            return mockClientStore.Object;
        }
    }
}
