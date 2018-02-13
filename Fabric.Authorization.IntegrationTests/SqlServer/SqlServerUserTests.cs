using Fabric.Authorization.API.Constants;
using Fabric.Authorization.IntegrationTests.Modules;
using Xunit;

namespace Fabric.Authorization.IntegrationTests.SqlServer
{
    [Collection("SqlServerTests")]
    public class SqlServerUserTests : UserTests
    {
        public SqlServerUserTests(IntegrationTestsFixture fixture, SqlServerIntegrationTestsFixture sqlFixture) : base(fixture, StorageProviders.SqlServer, sqlFixture.ConnectionStrings)
        {
        }
    }
}
