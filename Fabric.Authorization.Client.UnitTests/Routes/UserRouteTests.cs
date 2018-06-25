using Fabric.Authorization.Client.Routes;
using Xunit;

namespace Fabric.Authorization.Client.UnitTests.Routes
{
    public class UserRouteTests
    {
        [Fact]
        public void UserRouteBuilder_NoParameters_Success()
        {
            var route = new UserRouteBuilder().Route;
            Assert.Equal($"/{RouteConstants.UserRoute}", route);
        }

        [Fact]
        public void UserRouteBuilder_WithUserId_Success()
        {
            var route = new UserRouteBuilder().IdentityProvider("windows").SubjectId("first.last").Route;
            Assert.Equal($"/{RouteConstants.UserRoute}/windows/first.last", route);
        }

        [Fact]
        public void UserRouteBuilderPermissionsRoute_NoParameters_Success()
        {
            var route = new UserRouteBuilder().UserPermissionsRoute;
            Assert.Equal($"/{RouteConstants.UserRoute}/{RouteConstants.PermissionCollectionRoute}", route);
        }

        [Fact]
        public void UserRouteBuilderPermissionsRoute_WithUserId_Success()
        {
            var route = new UserRouteBuilder().IdentityProvider("windows").SubjectId("first.last").UserPermissionsRoute;
            Assert.Equal($"/{RouteConstants.UserRoute}/windows/first.last/{RouteConstants.PermissionCollectionRoute}", route);
        }

        [Fact]
        public void UserRouteBuilderGroupsRoute_WithUserId_Success()
        {
            var route = new UserRouteBuilder().IdentityProvider("windows").SubjectId("first.last").UserGroupsRoute;
            Assert.Equal($"/{RouteConstants.UserRoute}/windows/first.last/{RouteConstants.GroupCollectionRoute}", route);
        }

        [Fact]
        public void UserRouteBuilderRolesRoute_WithUserId_Success()
        {
            var route = new UserRouteBuilder().IdentityProvider("windows").SubjectId("first.last").UserRolesRoute;
            Assert.Equal($"/{RouteConstants.UserRoute}/windows/first.last/{RouteConstants.RoleCollectionRoute}", route);
        }
    }
}