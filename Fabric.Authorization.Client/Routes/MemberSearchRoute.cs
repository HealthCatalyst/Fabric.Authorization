namespace Fabric.Authorization.Client.Routes
{
    internal class MemberSearchRoute : BaseRoute
    {
        protected override string CollectionType { get; } = RouteConstants.MemberCollectionRoute;

        public override string ToString()
        {
            return BaseRouteSegment;
        }
    }

    internal class MemberSearchRouteBuilder
    {
        private readonly MemberSearchRoute _memberSearchRoute;

        public MemberSearchRouteBuilder()
        {
            _memberSearchRoute = new MemberSearchRoute();
        }

        public string Route => _memberSearchRoute.ToString();
    }
}