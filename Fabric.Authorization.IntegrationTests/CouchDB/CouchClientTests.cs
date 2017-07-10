namespace Fabric.Authorization.IntegrationTests.CouchDB
{
    public class CouchClientTests : ClientTests
    {
        public CouchClientTests() : base(useInMemoryDB: false)
        {
        }
    }
}