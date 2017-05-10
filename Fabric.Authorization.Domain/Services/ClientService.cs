using System;
using System.Collections.Generic;
using System.Linq;
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

        public bool DoesClientOwnResource(string clientId, string grain, string resource)
        {
            if (string.IsNullOrEmpty(clientId)) return false;

            var client = _clientStore.GetClient(clientId);
            var topLeveResource = client.TopLevelResource;

            if (topLeveResource == null)
            {
                return false;
            }

            
            if (TopLevelGrains.Contains(grain) && topLeveResource.Name == resource)
            {
                return true;
            }

            return HasRequestedResource(topLeveResource, grain, resource);
        }

        private bool HasRequestedResource(Resource parentResource, string grain, string resource)
        {
            var childResources = parentResource.Resources;

            if (childResources == null || childResources.Count == 0)
            {
                return false;
            }

            if (parentResource.Name == grain && childResources.Any(r => r.Name == resource))
            {
                return true;
            }

            return childResources.Any(childResource => HasRequestedResource(childResource, grain, resource));
        }
    }
}
