using System;
using System.Collections.Generic;
using System.Text;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.IntegrationTests.Modules;

namespace Fabric.Authorization.IntegrationTests.SqlServer
{
    public class SqlServerDosTests : DosTests
    {
        public SqlServerDosTests(IntegrationTestsFixture fixture) : base(fixture, StorageProviders.SqlServer)
        {
            
        }
    }
}
