using Fabric.Authorization.IntegrationTests.Modules;
using Xunit;

namespace Fabric.Authorization.IntegrationTests.CouchDB
{
    [Collection("CouchTests")]
    public class CouchDBUserTests : UserTests
    {
        public CouchDBUserTests() : base(false)
        {
        }
    }
}