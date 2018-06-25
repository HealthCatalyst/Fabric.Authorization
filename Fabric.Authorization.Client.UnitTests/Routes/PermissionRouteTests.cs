using System;
using Fabric.Authorization.Client.Routes;
using Xunit;

namespace Fabric.Authorization.Client.UnitTests.Routes
{
    public class PermissionsRouteTests
    {
        [Fact]
        public void PermissionRouteBuilder_NoParameters_Success()
        {
            var route = new PermissionRouteBuilder().Route;
            Assert.Equal($"/{RouteConstants.PermissionCollectionRoute}", route);
        }

        [Fact]
        public void PermissionRouteBuilder_WithPermissionId_Success()
        {
            var id = Guid.NewGuid();
            var route = new PermissionRouteBuilder().PermissionId(id.ToString()).Route;
            Assert.Equal($"/{RouteConstants.PermissionCollectionRoute}/{id}", route);
        }

        [Fact]
        public void PermissionRouteBuilder_WithGrainAndSecurableItem_Success()
        {
            var route = new PermissionRouteBuilder().Grain("app").SecurableItem("patientsafety").Route;
            Assert.Equal($"/{RouteConstants.PermissionCollectionRoute}/app/patientsafety", route);
        }

        [Fact]
        public void PermissionRouteBuilder_WithGrainAndSecurableItemAndName_Success()
        {
            var route = new PermissionRouteBuilder().Grain("app").SecurableItem("patientsafety").Name("permissionName").Route;
            Assert.Equal($"/{RouteConstants.PermissionCollectionRoute}/app/patientsafety/permissionName", route);
        }
    }
}