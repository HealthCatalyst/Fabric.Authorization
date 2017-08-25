using Fabric.Authorization.IntegrationTests.Modules;
using Xunit;

namespace Fabric.Authorization.IntegrationTests.CouchDB
{
    [Collection("CouchTests")]
    public class CouchClientTests : ClientTests
    {
        public CouchClientTests() : base(useInMemoryDB: false)
        {
        }
    }
}