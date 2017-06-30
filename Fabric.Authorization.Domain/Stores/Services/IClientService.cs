using Fabric.Authorization.Domain.Models;
using System.Collections.Generic;

namespace Fabric.Authorization.Domain.Services
{
    public interface IClientService
    {
        bool DoesClientOwnItem(string clientId, string grain, string securableItem);
        IEnumerable<Client> GetClients();

        Client GetClient(string id);

        Client AddClient(Client client);

        void DeleteClient(Client client);
    }
}
