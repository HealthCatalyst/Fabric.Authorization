using Fabric.Authorization.API.Constants;
using Fabric.Authorization.IntegrationTests.Services;
using Xunit;

namespace Fabric.Authorization.IntegrationTests.SqlServer
{
    [Collection("SqlServerTests")]
    public class SqlServerGroupMigratorServiceTests : GroupMigratorServiceTests
    {
        public SqlServerGroupMigratorServiceTests(
            IntegrationTestsFixture fixture,
            SqlServerIntegrationTestsFixture sqlFixture) : base(
                fixture,
                StorageProviders.SqlServer,
                sqlFixture.ConnectionStrings)
        {

        }
    }
}
