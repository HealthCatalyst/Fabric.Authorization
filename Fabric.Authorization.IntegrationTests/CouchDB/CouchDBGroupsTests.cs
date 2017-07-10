namespace Fabric.Authorization.IntegrationTests.CouchDB
{
    public class CouchDBGroupsTests : GroupsTests
    {
        public CouchDBGroupsTests() : base(useInMemoryDB: true)
        {
        }
    }
}