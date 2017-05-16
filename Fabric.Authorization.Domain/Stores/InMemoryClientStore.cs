using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores
{
    public class InMemoryClientStore : IClientStore
    {
        private static readonly ConcurrentDictionary<string, Client> Clients = new ConcurrentDictionary<string, Client>();

        static InMemoryClientStore()
        {
            var mvcClient = new Client
            {
                Id = "fabric-mvcsample",
                Name = "Sample Fabric MVC Client",
                TopLevelSecurableItem =  new SecurableItem
                {
                    Id = Guid.NewGuid(),
                    Name = "fabric-mvcsample"
                }
            };

            Clients.TryAdd(mvcClient.Id, mvcClient);
        }

        public IEnumerable<Client> GetClients()
        {
            return Clients.Values.AsEnumerable();
        }

        public Client GetClient(string clientId)
        {
            if (Clients.ContainsKey(clientId))
            {
                return Clients[clientId];
            }
            throw new ClientNotFoundException();
        }

        public bool ClientExists(string clientId)
        {
            return Clients.ContainsKey(clientId);
        }

        public Client Add(Client client)
        {
            client.CreatedDateTimeUtc = DateTime.UtcNow;
            Clients.TryAdd(client.Id, client);
            return client;
        }

        public void Delete(Client client)
        {
            client.IsDeleted = true;
            UpdateClient(client);
        }

        public void UpdateClient(Client client)
        {
            client.ModifiedDateTimeUtc = DateTime.UtcNow;
        }
    }
}
