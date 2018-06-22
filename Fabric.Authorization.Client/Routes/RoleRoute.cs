using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Fabric.Authorization.Client.UnitTests")]

namespace Fabric.Authorization.Client.Routes
{
    internal class RoleRoute : BaseRoute
    {
        protected override string CollectionType { get; } = RouteConstants.RoleCollectionRoute;

        public string RoleId { get; set; }
        public string Name { get; set; }
        public string Grain { get; set; }
        public string SecurableItem { get; set; }

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

            if (!string.IsNullOrEmpty(RoleId))
            {
                return $"{BaseRouteSegment}/{RoleId}";
            }

            return BaseRouteSegment;
        }
    }

    internal class RoleRouteBuilder
    {
        private readonly RoleRoute _roleRoute;

        public RoleRouteBuilder()
        {
            _roleRoute = new RoleRoute();
        }

        public RoleRouteBuilder RoleId(string roleId)
        {
            _roleRoute.RoleId = roleId;
            return this;
        }

        public RoleRouteBuilder Grain(string grain)
        {
            _roleRoute.Grain = grain;
            return this;
        }

        public RoleRouteBuilder SecurableItem(string securableItem)
        {
            _roleRoute.SecurableItem = securableItem;
            return this;
        }

        public RoleRouteBuilder Name(string name)
        {
            _roleRoute.Name = name;
            return this;
        }

        public string Route => _roleRoute.ToString();
        public string RolePermissionsRoute => $"{Route}/{RouteConstants.PermissionCollectionRoute}";
    }
}