using Fabric.Authorization.IntegrationTests.Modules;
using Xunit;

namespace Fabric.Authorization.IntegrationTests.CouchDB
{
    [Collection("CouchTests")]
    public class CouchDBGroupsTests : GroupsTests
    {
        public CouchDBGroupsTests() : base(false)
        {
        }
    }
}