using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fabric.Authorization.Domain;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;
using Moq;

namespace Fabric.Authorization.UnitTests.Mocks
{
    public static class ClientStoreMockExtensions
    {
        public static Mock<IClientStore> SetupGetClient(this Mock<IClientStore> mockClientStore, List<Client> clients)
        {
            mockClientStore.Setup(clientStore => clientStore.GetClient(It.IsAny<string>()))
                .Returns((string clientId) =>
                {
                    if (clients.Any(c => c.Id == clientId))
                    {
                        return clients.First(c => c.Id == clientId);
                    }
                    throw new ClientNotFoundException();
                });
            mockClientStore.Setup(clientStore => clientStore.ClientExists(It.IsAny<string>()))
                .Returns((string clientId) => clients.Any(c => c.Id == clientId));
            mockClientStore.Setup(clientStore => clientStore.GetClients())
                .Returns(() => clients);
            return mockClientStore;
        }

        public static Mock<IClientStore> SetupAddClient(this Mock<IClientStore> mockClientStore)
        {
            mockClientStore.Setup(clientStore => clientStore.Add(It.IsAny<Client>()))
                .Returns((Client c) =>
                {
                    c.CreatedDateTimeUtc = DateTime.UtcNow;
                    return c;
                });
            return mockClientStore;
        }

        public static IClientStore Create(this Mock<IClientStore> mockClientStore)
        {
            return mockClientStore.Object;
        }
    }
}
