using System;
using System.Linq;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;

namespace Fabric.Authorization.Domain.Services
{
    public class SecurableItemService : ISecurableItemService
    {
        private readonly IClientStore _clientStore;
        public SecurableItemService(IClientStore clientStore)
        {
            _clientStore = clientStore ?? throw new ArgumentNullException(nameof(clientStore));
        }
        public SecurableItem GetSecurableItem(string clientId, Guid itemId)
        {
            var topLevelSecurableItem = GetTopLevelSecurableItem(clientId);
            return GetSecurableItemById(topLevelSecurableItem, itemId);
        }

        public SecurableItem GetTopLevelSecurableItem(string clientId)
        {
            var client = _clientStore.Get(clientId);
            return client.TopLevelSecurableItem;
        }

        public SecurableItem AddSecurableItem(string clientId, SecurableItem item)
        {
            item.CreatedDateTimeUtc = DateTime.UtcNow;
            item.Id = Guid.NewGuid();
            var client = _clientStore.Get(clientId);
            CheckUniqueness(client.TopLevelSecurableItem, item);
            client.TopLevelSecurableItem.SecurableItems.Add(item);
            _clientStore.UpdateClient(client);
            return item;
        }

        public SecurableItem AddSecurableItem(string clientId, Guid itemId, SecurableItem item)
        {
            item.CreatedDateTimeUtc = DateTime.UtcNow;
            item.Id = Guid.NewGuid();
            var client = _clientStore.Get(clientId);
            var parentSecurableItem = GetSecurableItemById(client.TopLevelSecurableItem, itemId);
            CheckUniqueness(parentSecurableItem, item);
            parentSecurableItem.SecurableItems.Add(item);
            _clientStore.UpdateClient(client);
            return item;
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
                throw new SecurableItemNotFoundException();
            }

            var securableItem = childSecurableItems.FirstOrDefault(item => item.Id == itemId);
            if (securableItem != null)
            {
                return securableItem;
            }
            foreach (var childSecurableItem in childSecurableItems)
            {
                return GetSecurableItemById(childSecurableItem, itemId);
            }
            throw new SecurableItemNotFoundException();
        }

        private void CheckUniqueness(SecurableItem parentSecurableItem, SecurableItem item)
        {
            if (parentSecurableItem.Name == item.Name || parentSecurableItem.SecurableItems.Any(s => s.Name == item.Name))
            {
                throw new SecurableItemAlreadyExistsException(
                    $"The SecurableItem {item.Name} already exists within or has the same name as the parent item: {parentSecurableItem.Name}");
            }
        }
    }
}
