using System;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores.Services
{
    public class SecurableItemService
    {
        private readonly IClientStore _clientStore;

        public SecurableItemService(IClientStore clientStore)
        {
            _clientStore = clientStore ?? throw new ArgumentNullException(nameof(clientStore));
        }

        public async Task<SecurableItem> GetSecurableItem(string clientId, Guid itemId)
        {
            var topLevelSecurableItem = await this.GetTopLevelSecurableItem(clientId);
            return GetSecurableItemById(topLevelSecurableItem, itemId);
        }

        public async Task<SecurableItem> GetTopLevelSecurableItem(string clientId)
        {
            var client = await _clientStore.Get(clientId);
            return client.TopLevelSecurableItem;
        }

        public async Task<SecurableItem> AddSecurableItem(string clientId, SecurableItem item)
        {
            item.CreatedDateTimeUtc = DateTime.UtcNow;
            item.Id = Guid.NewGuid();
            var client = await _clientStore.Get(clientId);
            CheckUniqueness(client.TopLevelSecurableItem, item);
            client.TopLevelSecurableItem.SecurableItems.Add(item);
            await _clientStore.Update(client);
            return item;
        }

        public async Task<SecurableItem> AddSecurableItem(string clientId, Guid itemId, SecurableItem item)
        {
            item.CreatedDateTimeUtc = DateTime.UtcNow;
            item.Id = Guid.NewGuid();
            var client = await _clientStore.Get(clientId);
            var parentSecurableItem = GetSecurableItemById(client.TopLevelSecurableItem, itemId);
            CheckUniqueness(parentSecurableItem, item);
            parentSecurableItem.SecurableItems.Add(item);
            await _clientStore.Update(client);
            return item;
        }

        private bool TryGetSecurableItemById(SecurableItem parentSecurableItem, Guid itemId, out SecurableItem item)
        {
            try
            {
                item = GetSecurableItemById(parentSecurableItem, itemId);
                return true;
            }
            catch (AggregateException)
            {
                item = null;
                return false;
            }
        }

        private SecurableItem GetSecurableItemById(SecurableItem parentSecurableItem, Guid itemId)
        {
            if (parentSecurableItem.Id == itemId)
            {
                return parentSecurableItem;
            }

            var childSecurableItems = parentSecurableItem.SecurableItems;

            if (childSecurableItems == null || childSecurableItems.Count == 0)
            {
                throw new NotFoundException<SecurableItem>(itemId.ToString());
            }

            var securableItem = childSecurableItems.FirstOrDefault(item => item.Id == itemId);
            if (securableItem != null)
            {
                return securableItem;
            }

            foreach (var childSecurableItem in childSecurableItems)
            {
                if (TryGetSecurableItemById(childSecurableItem, itemId, out SecurableItem item))
                {
                    return item;
                }
            }

            throw new NotFoundException<SecurableItem>(itemId.ToString());
        }

        private void CheckUniqueness(SecurableItem parentSecurableItem, SecurableItem item)
        {
            if (parentSecurableItem.Name == item.Name || parentSecurableItem.SecurableItems.Any(s => s.Name == item.Name))
            {
                throw new AlreadyExistsException<SecurableItem>(
                    $"The SecurableItem {item.Name} already exists within or has the same name as the parent item: {parentSecurableItem.Name}");
            }
        }
    }
}