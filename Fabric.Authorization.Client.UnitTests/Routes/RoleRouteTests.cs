using System;
using Fabric.Authorization.Client.Routes;
using Xunit;

namespace Fabric.Authorization.Client.UnitTests.Routes
{
    public class RoleRouteTests
    {
        [Fact]
        public void RoleRouteBuilder_NoParameters_Success()
        {
            var route = new RoleRouteBuilder().Route;
            Assert.Equal($"/{RouteConstants.RoleCollectionRoute}", route);
        }

        [Fact]
        public void RoleRouteBuilder_WithRoleId_Success()
        {
            var id = Guid.NewGuid();
            var route = new RoleRouteBuilder().RoleId(id.ToString()).Route;
            Assert.Equal($"/{RouteConstants.RoleCollectionRoute}/{id}", route);
        }

        [Fact]
        public void RoleRouteBuilder_WithGrainAndSecurableItem_Success()
        {
            var route = new RoleRouteBuilder().Grain("app").SecurableItem("patientsafety").Route;
            Assert.Equal($"/{RouteConstants.RoleCollectionRoute}/app/patientsafety", route);
        }

        [Fact]
        public void RoleRouteBuilder_WithGrainAndSecurableItemAndName_Success()
        {
            var route = new RoleRouteBuilder().Grain("app").SecurableItem("patientsafety").Name("roleName").Route;
            Assert.Equal($"/{RouteConstants.RoleCollectionRoute}/app/patientsafety/roleName", route);
        }

        [Fact]
        public void RoleRouteBuilderPermissionsRoute_WithRoleId_Success()
        {
            var id = Guid.NewGuid();
            var route = new RoleRouteBuilder().RoleId(id.ToString()).RolePermissionsRoute;
            Assert.Equal($"/{RouteConstants.RoleCollectionRoute}/{id}/{RouteConstants.PermissionCollectionRoute}", route);
        }
    }
}