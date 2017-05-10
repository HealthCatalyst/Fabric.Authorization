using System.Security.Claims;

namespace Fabric.Authorization.UnitTests.Mocks
{
    public class TestIdentity : ClaimsIdentity
    {
        public TestIdentity(params Claim[] claims) : base(claims, "testauthentication")
        { }
    }
}
