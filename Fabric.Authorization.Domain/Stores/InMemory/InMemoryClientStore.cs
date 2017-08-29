using System;
using System.Collections.Concurrent;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores.InMemory
{
    public class InMemoryClientStore : InMemoryGenericStore<Client>, IClientStore
    {
        private readonly ConcurrentDictionary<string, Client> Clients = new ConcurrentDictionary<string, Client>();

        [Obsolete]
        public InMemoryClientStore()
        {
            var mvcClient = new Client
            {
                Id = "fabric-mvcsample",
                Name = "Sample Fabric MVC Client",
                TopLevelSecurableItem = new SecurableItem
                {
                    Id = Guid.NewGuid(),
                    Name = "fabric-mvcsample"
                }
            };

            Clients.TryAdd(mvcClient.Id, mvcClient);

            var angularClient = new Client
            {
                Id = "fabric-angularsample",
                Name = "Sample Fabric Angular Client",
                TopLevelSecurableItem = new SecurableItem
                {
                    Id = Guid.NewGuid(),
                    Name = "fabric-angularsample"
                }
            };

            this.Add(angularClient).Wait();
        }
    }
}