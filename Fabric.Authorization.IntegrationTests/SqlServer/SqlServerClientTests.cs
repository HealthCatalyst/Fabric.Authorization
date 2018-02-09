using System;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.IntegrationTests.Modules;
using Xunit;

namespace Fabric.Authorization.IntegrationTests.SqlServer
{
    [Collection("SqlServerTests")]
    public class SqlServerClientTests : ClientTests
    {
        public SqlServerClientTests(IntegrationTestsFixture fixture) : base(fixture, StorageProviders.SqlServer)
        {

        }
    }
}
