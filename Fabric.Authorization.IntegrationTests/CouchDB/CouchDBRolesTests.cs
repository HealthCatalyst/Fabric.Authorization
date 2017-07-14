using Xunit;

namespace Fabric.Authorization.IntegrationTests.CouchDB
{
    [Collection("CouchTests")]
    public class CouchDBRolesTests : RolesTests
    {
        public CouchDBRolesTests() : base(useInMemoryDB: false)
        {
        }
    }
}