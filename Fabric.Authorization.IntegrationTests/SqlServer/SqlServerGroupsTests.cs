using Fabric.Authorization.API.Constants;
using Fabric.Authorization.IntegrationTests.Modules;

namespace Fabric.Authorization.IntegrationTests.SqlServer
{
    public class SqlServerGroupsTests : GroupsTests
    {
        public SqlServerGroupsTests(IntegrationTestsFixture fixture) : base(fixture, StorageProviders.SqlServer)
        {
        }
    }
}
