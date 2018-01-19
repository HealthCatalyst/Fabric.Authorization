using Fabric.Authorization.API.Constants;
using Fabric.Authorization.IntegrationTests.Modules;
using Xunit;

namespace Fabric.Authorization.IntegrationTests.CouchDB
{
    [Collection("CouchTests")]
    public class CouchDbIdentitySearchTests : IdentitySearchTests
    {
        public CouchDbIdentitySearchTests(IdentitySearchFixture fixture) : base(fixture)
        {
            Fixture.Initialize(StorageProviders.CouchDb);
        }
    }
}