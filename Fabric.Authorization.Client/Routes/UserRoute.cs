using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Fabric.Authorization.Client.UnitTests")]

namespace Fabric.Authorization.Client.Routes
{
    internal class UserRoute : BaseRoute
    {
        protected override string CollectionType { get; } = RouteConstants.UserRoute;

        public string IdentityProvider { get; set; }
        public string SubjectId { get; set; }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(IdentityProvider) && string.IsNullOrEmpty(SubjectId))
            {
                return BaseRouteSegment;
            }

            return $"{BaseRouteSegment}/{IdentityProvider}/{SubjectId}";
        }
    }

    internal class UserRouteBuilder
    {
        private readonly UserRoute _userRoute;

        public UserRouteBuilder()
        {
            _userRoute = new UserRoute();
        }

        public UserRouteBuilder IdentityProvider(string identityProvider)
        {
            _userRoute.IdentityProvider = identityProvider;
            return this;
        }

        public UserRouteBuilder SubjectId(string subjectId)
        {
            _userRoute.SubjectId = subjectId;
            return this;
        }

        public string Route => _userRoute.ToString();
        public string UserPermissionsRoute => $"{Route}/{RouteConstants.PermissionCollectionRoute}";
        public string UserRolesRoute => $"{Route}/{RouteConstants.RoleCollectionRoute}";
        public string UserGroupsRoute => $"{Route}/{RouteConstants.GroupCollectionRoute}";
    }
}