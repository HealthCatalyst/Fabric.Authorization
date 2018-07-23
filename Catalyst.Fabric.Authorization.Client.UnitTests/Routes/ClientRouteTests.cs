using Catalyst.Fabric.Authorization.Client.Routes;
using Xunit;

namespace Catalyst.Fabric.Authorization.Client.UnitTests.Routes
{
    public class ClientRouteTests
    {
        [Fact]
        public void ClientRouteBuilder_NoClientId_Success()
        {
            var route = new ClientRouteBuilder().Route;
            Assert.Equal($"/{RouteConstants.ClientCollectionRoute}", route);
        }

        [Fact]
        public void ClientRouteBuilder_WithClientId_Success()
        {
            var route = new ClientRouteBuilder().ClientId("client_id").Route;
            Assert.Equal($"/{RouteConstants.ClientCollectionRoute}/client_id", route);
        }
    }
}