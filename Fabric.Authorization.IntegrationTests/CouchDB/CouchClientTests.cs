using Fabric.Authorization.API.Constants;
using Fabric.Authorization.IntegrationTests.Modules;
using Xunit;

namespace Fabric.Authorization.IntegrationTests.CouchDB
{
    [Collection("CouchTests")]
    public class CouchClientTests : ClientTests
    {
        public CouchClientTests(IntegrationTestsFixture fixture) : base(fixture, StorageProviders.CouchDb)
        {
        }
    }
}