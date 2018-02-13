using System;
using System.Collections.Generic;
using System.Text;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.IntegrationTests.Modules;
using Xunit;

namespace Fabric.Authorization.IntegrationTests.SqlServer
{
    [Collection("SqlServerTests")]
    public class SqlServerDosTests : DosTests
    {
        public SqlServerDosTests(IntegrationTestsFixture fixture, SqlServerIntegrationTestsFixture sqlFixture) : base(fixture, StorageProviders.SqlServer, sqlFixture.ConnectionStrings)
        {
        }
    }
}
