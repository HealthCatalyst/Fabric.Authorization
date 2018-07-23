using Catalyst.Fabric.Authorization.Client.Routes;
using Xunit;

namespace Catalyst.Fabric.Authorization.Client.UnitTests.Routes
{
    public class GroupRouteTests
    {
        [Fact]
        public void GroupRouteBuilder_NoParameters_Success()
        {
            var route = new GroupRouteBuilder().Route;
            Assert.Equal($"/{RouteConstants.GroupCollectionRoute}", route);
        }

        [Fact]
        public void GroupRouteBuilder_WithGroupName_Success()
        {
            var route = new GroupRouteBuilder().Name("groupName").Route;
            Assert.Equal($"/{RouteConstants.GroupCollectionRoute}/groupName", route);
        }

        [Fact]
        public void GroupRouteBuilderRolesRoute_WithGroupNameAndGrainAndSecurableItem_Success()
        {
            var route = new GroupRouteBuilder().Name("groupName").Grain("app").SecurableItem("patientsafety").GroupRolesRoute;
            Assert.Equal($"/{RouteConstants.GroupCollectionRoute}/groupName/app/patientsafety/{RouteConstants.RoleCollectionRoute}", route);
        }

        [Fact]
        public void GroupRouteBuilderRolesRoute_WithGroupNameAndSecurableItemAndNoGrain_Success()
        {
            var route = new GroupRouteBuilder().Name("groupName").SecurableItem("patientsafety").GroupRolesRoute;
            Assert.Equal($"/{RouteConstants.GroupCollectionRoute}/groupName/{RouteConstants.RoleCollectionRoute}", route);
        }

        [Fact]
        public void GroupRouteBuilderUsersRoute_WithGroupName_Success()
        {
            var route = new GroupRouteBuilder().Name("groupName").GroupUsersRoute;
            Assert.Equal($"/{RouteConstants.GroupCollectionRoute}/groupName/{RouteConstants.UserCollectionRoute}", route);
        }
    }
}