using Fabric.Authorization.API.Constants;
using Fabric.Authorization.IntegrationTests.Modules;
using Xunit;

namespace Fabric.Authorization.IntegrationTests.SqlServer
{
    [Collection("SqlServerTests")]
    public class SqlServerPermissionsTests //: PermissionsTests
    {
        public SqlServerPermissionsTests(IntegrationTestsFixture fixture) //: base(fixture, StorageProviders.SqlServer)
        {
        }
    }
}
