namespace Catalyst.Fabric.Authorization.Client.Routes
{
    internal class ClientRoute : BaseRoute
    {
        protected override string CollectionType { get; } = RouteConstants.ClientCollectionRoute;

        public string ClientId { get; set; }

        public override string ToString()
        {
            return !string.IsNullOrEmpty(ClientId)
                ? $"{BaseRouteSegment}/{ClientId}"
                : $"{BaseRouteSegment}";
        }
    }

    internal class ClientRouteBuilder
    {
        private readonly ClientRoute _clientRoute;

        public ClientRouteBuilder()
        {
            _clientRoute = new ClientRoute();
        }

        public ClientRouteBuilder ClientId(string clientId)
        {
            _clientRoute.ClientId = clientId;
            return this;
        }

        public string Route => _clientRoute.ToString();
    }
}