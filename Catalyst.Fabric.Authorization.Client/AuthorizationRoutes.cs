namespace Catalyst.Fabric.Authorization.Client
{
    internal static class AuthorizationRoutes
    {
        private static readonly string userPermission = "/user/permissions";

        public static string GetUserPermissionUrl()
        {
            return userPermission;
        }
    }
}