using Fabric.Authorization.API.Constants;
using Fabric.Authorization.IntegrationTests.Modules;
using Xunit;

namespace Fabric.Authorization.IntegrationTests.SqlServer
{
    [Collection("SqlServerTests")]
    public class SqlServerRolesTests : RolesTests
    {
        public SqlServerRolesTests(IntegrationTestsFixture fixture) : base(fixture, StorageProviders.SqlServer)
        {
        }
    }
}
