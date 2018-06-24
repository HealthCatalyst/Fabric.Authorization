using Fabric.Authorization.Client.Routes;
using Xunit;

namespace Fabric.Authorization.Client.UnitTests.Routes
{
    public class MemberSearchRouteTests
    {
        [Fact]
        public void MemberSearchBuilderRoute_NoParameters_Success()
        {
            var route = new MemberSearchRouteBuilder().Route;
            Assert.Equal($"/{RouteConstants.MemberCollectionRoute}", route);
        }
    }
}