using System;
using System.Collections.Concurrent;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;

namespace Fabric.Authorization.Persistence.InMemory.Stores
{
    public class InMemoryClientStore : InMemoryGenericStore<Client>, IClientStore
    {
        private readonly ConcurrentDictionary<string, Client> _clients = new ConcurrentDictionary<string, Client>();
        
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

            _clients.TryAdd(mvcClient.Id, mvcClient);

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