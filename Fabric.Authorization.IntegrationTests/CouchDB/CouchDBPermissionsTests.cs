using Xunit;

namespace Fabric.Authorization.IntegrationTests.CouchDB
{
    [Collection("CouchTests")]
    public class CouchDBPermissionsTests : PermissionsTests
    {
        public CouchDBPermissionsTests() : base(useInMemoryDB: false)
        {
        }
    }
}