namespace Fabric.Authorization.Client.Routes
{
    internal class SecurableItemRoute : BaseRoute
    {
        protected override string CollectionType { get; } = RouteConstants.SecurableItemCollectionRoute;

        public string SecurableItemId { get; set; }

        public override string ToString()
        {
            return !string.IsNullOrEmpty(SecurableItemId)
                ? $"{BaseRouteSegment}/{SecurableItemId}"
                : BaseRouteSegment;
        }
    }

    internal class SecurableItemRouteBuilder
    {
        private readonly SecurableItemRoute _securableItemRoute;

        public SecurableItemRouteBuilder()
        {
            _securableItemRoute = new SecurableItemRoute();
        }

        public SecurableItemRoute SecurableItemId(string securableItemId)
        {
            _securableItemRoute.SecurableItemId = securableItemId;
            return _securableItemRoute;
        }
    }
}