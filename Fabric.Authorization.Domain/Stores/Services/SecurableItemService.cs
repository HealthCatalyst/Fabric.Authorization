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
            _clientStore.Update(client);
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
            _clientStore.Update(client);
            return item;
        }

        private bool TryGetSecurableItemById(SecurableItem parentSecurableItem, Guid itemId, out SecurableItem item)
        {
            try
            {
                item = GetSecurableItemById(parentSecurableItem, itemId);
                return true;
            }
            catch(NotFoundException<SecurableItem>)
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
                SecurableItem item;
                if (TryGetSecurableItemById(childSecurableItem, itemId, out item))
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
