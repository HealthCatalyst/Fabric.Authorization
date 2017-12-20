﻿using Fabric.Authorization.IntegrationTests.Modules;
using Xunit;

namespace Fabric.Authorization.IntegrationTests.CouchDB
{
    [Collection("CouchTests")]
    public class CouchDBPermissionsTests : PermissionsTests
    {
        public CouchDBPermissionsTests(IntegrationTestsFixture fixture) : base(fixture, useInMemoryDb: false)
        {
        }
    }
}