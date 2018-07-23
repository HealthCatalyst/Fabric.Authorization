using Catalyst.Fabric.Authorization.Client.Routes;
using Xunit;

namespace Catalyst.Fabric.Authorization.Client.UnitTests.Routes
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

        [Fact]
        public void UserPermissionsRouteBuilder_NoOptions_Success()
        {
            var actualRoute = new UserRouteBuilder().UserPermissionsRoute;
            var expectedRoute = $"/{RouteConstants.UserRoute}/{RouteConstants.PermissionCollectionRoute}";
            Assert.Equal(expectedRoute, actualRoute);
        }

        [Fact]
        public void UserPermissionsRouteBuilder_WithSecurableItem_Success()
        {
            var securableItem = "unit-test";
            var actualRoute = new UserRouteBuilder().SecurableItem(securableItem).UserPermissionsRoute;
            var expectedRoute = $"/{RouteConstants.UserRoute}/{RouteConstants.PermissionCollectionRoute}?{ClientConstants.SecurableItem}={securableItem}";
            Assert.Equal(expectedRoute, actualRoute);
        }

        [Fact]
        public void UserPermissionsRouteBuilder_WithGrain_Success()
        {
            var grain = "app";
            var actualRoute = new UserRouteBuilder().Grain(grain).UserPermissionsRoute;
            var expectedRoute = $"/{RouteConstants.UserRoute}/{RouteConstants.PermissionCollectionRoute}?{ClientConstants.Grain}={grain}";
            Assert.Equal(expectedRoute, actualRoute);
        }

        [Fact]
        public void UserPermissionsRouteBuilder_WithGrainAndSecurableItem_Success()
        {
            var grain = "app";
            var securableItem = "unit-test";
            var actualRoute = new UserRouteBuilder().Grain(grain).SecurableItem(securableItem).UserPermissionsRoute;
            var expectedRoute = $"/{RouteConstants.UserRoute}/{RouteConstants.PermissionCollectionRoute}?{ClientConstants.Grain}={grain}&{ClientConstants.SecurableItem}={securableItem}";
            Assert.Equal(expectedRoute, actualRoute);
        }
    }
}