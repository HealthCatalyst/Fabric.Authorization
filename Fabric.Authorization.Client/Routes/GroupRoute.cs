namespace Fabric.Authorization.Client.Routes
{
    internal class GroupRoute : BaseRoute
    {
        protected override string CollectionType { get; } = RouteConstants.GroupCollectionRoute;
        public static string BatchUpdateRoute { get; } = "/UpdateGroups";

        public string Name { get; set; }
        public string Grain { get; set; }
        public string SecurableItem { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Name))
            {
                var route = $"{BaseRouteSegment}/{Name}";
                if (!string.IsNullOrEmpty(Grain))
                {
                    route = $"{route}/{Grain}";
                    if (!string.IsNullOrEmpty(SecurableItem))
                    {
                        route = $"{route}/{SecurableItem}";
                    }
                }

                return route;
            }

            return BaseRouteSegment;
        }
    }

    internal class GroupRouteBuilder
    {
        private readonly GroupRoute _groupRoute;

        public GroupRouteBuilder()
        {
            _groupRoute = new GroupRoute();
        }

        public GroupRoute Name(string name)
        {
            _groupRoute.Name = name;
            return _groupRoute;
        }

        public GroupRoute Grain(string grain)
        {
            _groupRoute.Grain = grain;
            return _groupRoute;
        }

        public GroupRoute SecurableItem(string securableItem)
        {
            _groupRoute.SecurableItem = securableItem;
            return _groupRoute;
        }

        public string Route => _groupRoute.ToString();
        public string GroupRolesRoute => $"{Route}/{RouteConstants.RoleCollectionRoute}";
        public string GroupUsersRoute => $"{Route}/{RouteConstants.UserCollectionRoute}";

        public string BatchUpdateRoute => GroupRoute.BatchUpdateRoute;
    }
}