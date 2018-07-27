using Catalyst.Fabric.Authorization.Client.Routes;
using Xunit;

namespace Catalyst.Fabric.Authorization.Client.UnitTests.Routes
{
    public class MemberSearchRouteTests
    {
        [Fact]
        public void MemberSearchBuilderRoute_NoParameters_Success()
        {
            var route = new MemberSearchRouteBuilder().Route;
            Assert.Equal($"{RouteConstants.MemberCollectionRoute}", route);
        }
    }
}