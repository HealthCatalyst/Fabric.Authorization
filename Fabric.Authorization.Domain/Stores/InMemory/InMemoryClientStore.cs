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

            Clients.TryAdd(angularClient.Id, angularClient);
        }

        public IEnumerable<Client> GetAll()
        {
            return Clients.Values.AsEnumerable();
        }

        public Client Get(string clientId)
        {
            if (this.Exists(clientId))
            {
                return Clients[clientId];
            }

            throw new ClientNotFoundException(clientId);
        }

        public bool Exists(string clientId)
        {
            return Clients.ContainsKey(clientId);
        }

        public Client Add(Client client)
        {
            client.Track(true);
            
            if (this.Exists(client.Id))
            {
                throw new ClientAlreadyExistsException(client.Id);
            }

            if (!Clients.TryAdd(client.Id, client))
            {
                throw new CouldNotCompleteOperationException();
            }

            return client;
        }

        public void Delete(Client client)
        {
            client.IsDeleted = true;
            UpdateClient(client);
        }

        public void UpdateClient(Client client)
        {
            client.Track();

            if (this.Exists(client.Id) )
            {
                if(!Clients.TryUpdate(client.Id, client, this.Get(client.Id)))
                {
                    throw new CouldNotCompleteOperationException();
                }
            }
            else
            {
                throw new ClientNotFoundException(client.Id.ToString());
            }
        }
    }
}
