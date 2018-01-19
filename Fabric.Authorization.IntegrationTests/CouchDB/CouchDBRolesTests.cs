using Fabric.Authorization.API.Constants;
using Fabric.Authorization.IntegrationTests.Modules;
using Xunit;

namespace Fabric.Authorization.IntegrationTests.CouchDB
{
    [Collection("CouchTests")]
    public class CouchDBRolesTests //: RolesTests, IClassFixture<IntegrationTestsFixture>
    {
        public CouchDBRolesTests(IntegrationTestsFixture fixture)// : base(fixture, StorageProviders.CouchDb)
        {
        }
    }
}