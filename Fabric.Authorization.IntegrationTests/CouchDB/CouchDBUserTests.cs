using Xunit;

namespace Fabric.Authorization.IntegrationTests.CouchDB
{
    [Collection("CouchTests")]
    public class CouchDBUserTests : UserTests
    {
        public CouchDBUserTests() : base(useInMemoryDB: false)
        {
        }
    }
}