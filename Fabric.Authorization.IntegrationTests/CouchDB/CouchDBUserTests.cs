namespace Fabric.Authorization.IntegrationTests.CouchDB
{
    public class CouchDBUserTests : UserTests
    {
        public CouchDBUserTests() : base(useInMemoryDB: false)
        {
        }
    }
}