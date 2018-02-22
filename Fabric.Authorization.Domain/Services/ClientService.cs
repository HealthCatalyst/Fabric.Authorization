using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;

namespace Fabric.Authorization.Domain.Services
{
    public class ClientService
    {
        private static readonly List<string> TopLevelGrains = new List<string>
        {
            "app",
            "patient",
            "user"
        };

        private readonly IClientStore _clientStore;
        private readonly ISecurableItemStore _securableItemStore;

        public ClientService(IClientStore clientStore, ISecurableItemStore securableItemStore)
        {
            _clientStore = clientStore ?? throw new ArgumentNullException(nameof(clientStore));
            _securableItemStore = securableItemStore ?? throw new ArgumentNullException(nameof(securableItemStore));
        }

        /// <summary>
        /// All ownership checks should pass through this method since it first checks the ClientOwner property of the securable item first.
        /// </summary>
        /// <param name="clientId">Unique client ID</param>
        /// <param name="grain">Entity grain</param>
        /// <param name="securableItem">Entity securable item</param>
        /// <returns>True if client owns item; otherwise false</returns>
        public async Task<bool> DoesClientOwnItem(string clientId, string grain, string securableItem)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                return false;
            }

            if (await IsClientOwner(clientId, securableItem))
            {
                return true;
            }

            var client = await _clientStore.Get(clientId);
            return DoesClientOwnItem(client, grain, securableItem);
        }

        private bool DoesClientOwnItem(Client client, string grain, string securableItem)
        {
            var topLevelSecurableItem = client.TopLevelSecurableItem;
            if (topLevelSecurableItem == null)
            {
                return false;
            }

            if (TopLevelGrains.Contains(grain) && topLevelSecurableItem.Name == securableItem)
            {
                return true;
            }

            return HasRequestedSecurableItem(topLevelSecurableItem, grain, securableItem);
        }

        private async Task<bool> IsClientOwner(string clientId, string securableItem)
        {

            var item = await _securableItemStore.Get(securableItem);
            return item?.ClientOwner == clientId;
        }

        public async Task<IEnumerable<Client>> GetClients(bool includeDeleted = false)
        {
            var clients = await _clientStore.GetAll();

            return clients.Where(c => !c.IsDeleted || includeDeleted);
        }

        public async Task<Client> GetClient(string id)
        {
            return await _clientStore.Get(id);
        }

        public async Task<Client> AddClient(Client client)
        {
            client.TopLevelSecurableItem = new SecurableItem
            {
                Id = Guid.NewGuid(),
                Name = client.Id,
                ClientOwner = client.Id,
                Grain = Defaults.Authorization.AppGrain
            };

            return await _clientStore.Add(client);
        }

        public async Task DeleteClient(Client client)
        {
            await _clientStore.Delete(client);
        }

        private static bool HasRequestedSecurableItem(SecurableItem parentSecurableItem, string grain, string securableItem)
        {
            var childSecurableItems = parentSecurableItem.SecurableItems;

            if (childSecurableItems == null || childSecurableItems.Count == 0)
            {
                return false;
            }

            if (parentSecurableItem.Name == grain && childSecurableItems.Any(r => r.Name == securableItem))
            {
                return true;
            }

            return childSecurableItems.Any(
                childSecurableItem => HasRequestedSecurableItem(childSecurableItem, grain, securableItem));
        }

        public async Task<bool> Exists(string id)
        {
            return await _clientStore.Exists(id).ConfigureAwait(false);
        }
    }
}