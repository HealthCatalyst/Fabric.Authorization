namespace Fabric.Authorization.Client.Routes
{
    internal class UserRoute : BaseRoute
    {
        public static string BaseRoute { get; } = $"/{RouteConstants.UserRoute}";

        public string IdentityProvider { get; set; }
        public string SubjectId { get; set; }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(IdentityProvider) && string.IsNullOrEmpty(SubjectId))
            {
                return BaseRoute;
            }

            return $"{BaseRoute}/{IdentityProvider}/{SubjectId}";
        }
    }

    internal class UserRouteBuilder
    {
        private readonly UserRoute _userRoute;

        public UserRouteBuilder()
        {
            _userRoute = new UserRoute();
        }

        public UserRoute IdentityProvider(string identityProvider)
        {
            _userRoute.IdentityProvider = identityProvider;
            return _userRoute;
        }

        public UserRoute SubjectId(string subjectId)
        {
            _userRoute.SubjectId = subjectId;
            return _userRoute;
        }

        public string Route => _userRoute.ToString();
        public string UserPermissionsRoute => $"{Route}/{RouteConstants.PermissionCollectionRoute}";
        public string UserRolesRoute => $"{Route}/{RouteConstants.RoleCollectionRoute}";
        public string UserGroupsRoute => $"{Route}/{RouteConstants.GroupCollectionRoute}";
    }
}