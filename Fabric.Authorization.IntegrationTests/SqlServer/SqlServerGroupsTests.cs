using Fabric.Authorization.API.Constants;
using Fabric.Authorization.IntegrationTests.Modules;
using Xunit;

namespace Fabric.Authorization.IntegrationTests.SqlServer
{
    [Collection("SqlServerTests")]
    public class SqlServerGroupsTests //: GroupsTests
    {
        public SqlServerGroupsTests(IntegrationTestsFixture fixture) //: base(fixture, StorageProviders.SqlServer)
        {
        }
    }
}
