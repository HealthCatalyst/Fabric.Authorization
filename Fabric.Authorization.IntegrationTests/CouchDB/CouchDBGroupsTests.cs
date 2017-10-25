using Fabric.Authorization.IntegrationTests.Modules;
using Nancy;
using Xunit;

namespace Fabric.Authorization.IntegrationTests.CouchDB
{
    [Collection("CouchTests")]
    public class CouchDBGroupsTests : GroupsTests
    {
        public CouchDBGroupsTests() : base(false)
        {
        }

       /* [Fact]
        [DisplayTestMethodName]
        public void AddGroup_ActiveGroupWithOldIdExists_BadRequest()
        {

        }

        [Fact]
        [DisplayTestMethodName]
        public void AddGroup_InactiveGroupWithOldIdExists_Success()
        {

        }*/
    }
}