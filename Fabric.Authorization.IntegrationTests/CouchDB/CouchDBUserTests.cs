using Fabric.Authorization.API.Constants;
using Fabric.Authorization.IntegrationTests.Modules;
using Xunit;

namespace Fabric.Authorization.IntegrationTests.CouchDB
{
    [Collection("CouchTests")]
    public class CouchDBUserTests : UserTests, IClassFixture<IntegrationTestsFixture>
    {
        public CouchDBUserTests(IntegrationTestsFixture fixture) : base(fixture, StorageProviders.CouchDb)
        {
        }
    }
}