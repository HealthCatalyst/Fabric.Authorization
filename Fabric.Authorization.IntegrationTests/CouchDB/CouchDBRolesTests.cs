namespace Fabric.Authorization.IntegrationTests.CouchDB
{
    public class CouchDBRolesTests : RolesTests
    {
        public CouchDBRolesTests() : base(useInMemoryDB: true)
        {
        }
    }
}