namespace Fabric.Authorization.Client.Routes
{
    internal class PermissionRoute : BaseRoute
    {
        protected override string CollectionType { get; } = RouteConstants.PermissionCollectionRoute;

        public string PermissionId { get; set; }
        public string Grain { get; set; }
        public string SecurableItem { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Grain))
            {
                var route = $"{BaseRouteSegment}/{Grain}";
                if (!string.IsNullOrEmpty(SecurableItem))
                {
                    route = $"{route}/{SecurableItem}";

                    if (!string.IsNullOrEmpty(Name))
                    {
                        route = $"{route}/{Name}";
                    }
                }

                return route;
            }

            return !string.IsNullOrEmpty(PermissionId)
                ? $"{BaseRouteSegment}/{PermissionId}"
                : BaseRouteSegment;
        }
    }

    internal class PermissionRouteBuilder
    {
        private readonly PermissionRoute _permissionRoute;

        public PermissionRouteBuilder()
        {
            _permissionRoute = new PermissionRoute();
        }

        public PermissionRouteBuilder PermissionId(string permissionId)
        {
            _permissionRoute.PermissionId = permissionId;
            return this;
        }

        public PermissionRouteBuilder Grain(string grain)
        {
            _permissionRoute.Grain = grain;
            return this;
        }

        public PermissionRouteBuilder SecurableItem(string securableItem)
        {
            _permissionRoute.SecurableItem = securableItem;
            return this;
        }

        public PermissionRouteBuilder Name(string name)
        {
            _permissionRoute.Name = name;
            return this;
        }

        public string Route => _permissionRoute.ToString();
    }
}