using System.Security.Claims;
using IdentityModel;

namespace Fabric.Authorization.UnitTests.Mocks
{
    public class TestIdentity : ClaimsIdentity
    {
        public TestIdentity(params Claim[] claims) : base(claims, "testauthentication", JwtClaimTypes.Name, JwtClaimTypes.Role)
        {
        }
    }
}
