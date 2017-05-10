using System.Collections.Concurrent;
using Fabric.Authorization.Domain.Exceptions;

namespace Fabric.Authorization.Domain.Stores
{
    public class InMemoryClientStore : IClientStore
    {
        private static readonly ConcurrentDictionary<string, Client> Clients = new ConcurrentDictionary<string, Client>();
        public Client GetClient(string clientId)
        {
            if (Clients.ContainsKey(clientId))
            {
                return Clients[clientId];
            }
            throw new ClientNotFoundException();
        }

        public Client Add(Client client)
        {
            Clients.TryAdd(client.Id, client);
            return client;
        }
    }
}
