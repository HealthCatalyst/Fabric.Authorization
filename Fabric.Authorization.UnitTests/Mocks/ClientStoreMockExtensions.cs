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
    public static class ClientStoreMockExtensions
    {
        public static Mock<IClientStore> SetupGetClient(this Mock<IClientStore> mockClientStore, List<Client> clients)
        {
            mockClientStore.Setup(clientStore => clientStore.Get(It.IsAny<string>()))
                .Returns((string clientId) =>
                {
                    if (clients.Any(c => c.Id == clientId))
                    {
                        return Task.FromResult(clients.First(c => c.Id == clientId));
                    }
                    throw new NotFoundException<Client>();
                });
            mockClientStore.Setup(clientStore => clientStore.Exists(It.IsAny<string>()))
                .Returns((string clientId) => Task.FromResult(clients.Any(c => c.Id == clientId)));
            mockClientStore.Setup(clientStore => clientStore.GetAll())
                .Returns(() => Task.FromResult(clients.AsEnumerable()));
            return mockClientStore;
        }

        public static Mock<IClientStore> SetupAddClient(this Mock<IClientStore> mockClientStore)
        {
            mockClientStore.Setup(clientStore => clientStore.Add(It.IsAny<Client>()))
                .Returns((Client c) =>
                {
                    c.CreatedDateTimeUtc = DateTime.UtcNow;
                    return Task.FromResult(c);
                });
            return mockClientStore;
        }

        public static Mock<IClientStore> SetupDeleteClient(this Mock<IClientStore> mockClientStore)
        {
            mockClientStore.Setup(clientStore => clientStore.Delete(It.IsAny<Client>())).Verifiable();
            return mockClientStore;
        }

        public static IClientStore Create(this Mock<IClientStore> mockClientStore)
        {
            return mockClientStore.Object;
        }
    }
}
