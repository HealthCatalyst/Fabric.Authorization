﻿using System;
using System.Collections.Generic;
using System.Linq;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;

namespace Fabric.Authorization.Domain.Services
{
    public class ClientService : IClientService
    {
        public static readonly List<string> TopLevelGrains = new List<string>
        {
            "app",
            "patient",
            "user"
        };

        private readonly IClientStore _clientStore;

        public ClientService(IClientStore clientStore)
        {
            _clientStore = clientStore ?? throw new ArgumentNullException(nameof(clientStore));
        }

        public bool DoesClientOwnItem(string clientId, string grain, string securableItem)
        {
            if (string.IsNullOrEmpty(clientId)) return false;

            var client = _clientStore.GetClient(clientId);
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

        private bool HasRequestedSecurableItem(SecurableItem parentSecurableItem, string grain, string securableItem)
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

            return childSecurableItems.Any(childSecurableItem => HasRequestedSecurableItem(childSecurableItem, grain, securableItem));
        }
    }
}
