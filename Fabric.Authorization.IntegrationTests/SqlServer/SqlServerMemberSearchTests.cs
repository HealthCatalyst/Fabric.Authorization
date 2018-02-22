using Fabric.Authorization.API.Constants;
using Fabric.Authorization.IntegrationTests.Modules;
using Xunit;

namespace Fabric.Authorization.IntegrationTests.SqlServer
{
    [Collection("SqlServerTests")]
    public class SqlServerMemberSearchTests : MemberSearchTests
    {
        public SqlServerMemberSearchTests(MemberSearchFixture fixture, SqlServerIntegrationTestsFixture sqlFixture) : base(fixture)
        {
            fixture.ConnectionStrings = sqlFixture.ConnectionStrings;
            Fixture.Initialize(StorageProviders.SqlServer);
        }
    }
}
