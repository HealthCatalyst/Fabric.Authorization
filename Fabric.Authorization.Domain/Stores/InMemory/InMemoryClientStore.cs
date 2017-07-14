using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores
{
    public class InMemoryClientStore : IClientStore
    {
        private readonly ConcurrentDictionary<string, Client> Clients = new ConcurrentDictionary<string, Client>();

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

            Clients.TryAdd(angularClient.Id, angularClient);
        }

        public Task<IEnumerable<Client>> GetAll()
        {
            return Task.FromResult(Clients.Values.Where(c => !c.IsDeleted));
        }

        public async Task<Client> Get(string clientId)
        {
            if (await this.Exists(clientId) && !Clients[clientId].IsDeleted)
            {
                return Clients[clientId];
            }

            throw new NotFoundException<Client>(clientId);
        }

        public Task<bool> Exists(string clientId) => Task.FromResult(Clients.ContainsKey(clientId));

        public async Task<Client> Add(Client client)
        {
            client.Track(true);

            if (await this.Exists(client.Id))
            {
                throw new AlreadyExistsException<Client>(client, client.Id);
            }

            if (!Clients.TryAdd(client.Id, client))
            {
                throw new CouldNotCompleteOperationException();
            }

            return client;
        }

        public async Task Delete(Client client)
        {
            client.IsDeleted = true;
            await Update(client);
        }

        public async Task Update(Client client)
        {
            client.Track();

            if (await this.Exists(client.Id))
            {
                if (!Clients.TryUpdate(client.Id, client, Clients[client.Id]))
                {
                    throw new CouldNotCompleteOperationException();
                }
            }
            else
            {
                throw new NotFoundException<Client>(client.Id.ToString());
            }
        }
    }
}