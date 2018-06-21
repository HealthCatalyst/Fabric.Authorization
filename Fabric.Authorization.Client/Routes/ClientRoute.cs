﻿namespace Fabric.Authorization.Client.Routes
{
    internal class ClientRoute : BaseRoute
    {
        public string BaseRoute { get; } = $"/{RouteConstants.ClientCollectionRoute}";
        public string ClientId { get; set; }

        public override string ToString()
        {
            return string.IsNullOrEmpty(ClientId)
                ? $"{BaseRoute}"
                : $"{BaseRoute}/{ClientId}";
        }
    }

    internal class ClientRouteBuilder
    {
        private readonly ClientRoute _clientRoute;

        public ClientRouteBuilder()
        {
            _clientRoute = new ClientRoute();
        }

        public ClientRoute ClientId(string clientId)
        {
            _clientRoute.ClientId = clientId;
            return _clientRoute;
        }

        public string Route => _clientRoute.ToString();
    }
}