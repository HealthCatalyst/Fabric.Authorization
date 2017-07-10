namespace Fabric.Authorization.IntegrationTests.CouchDB
{
    public class CouchDBPermissionsTests : PermissionsTests
    {
        public CouchDBPermissionsTests() : base(useInMemoryDB: false)
        {
        }
    }
}