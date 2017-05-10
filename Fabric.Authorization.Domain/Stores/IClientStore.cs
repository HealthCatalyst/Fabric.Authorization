using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores
{
    public interface IClientStore
    {
        IEnumerable<Client> GetClients();
        bool ClientExists(string clientId);
        Client GetClient(string clientId);
        Client Add(Client client);

        void Delete(Client client);

        void UpdateClient(Client client);
    }
}
