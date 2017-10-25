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

        [Fact]
        [DisplayTestMethodName]
        public void AddGroup_DuplicateGroupExistsAndDeleted_Success()
        {
            const string groupName = "Group1";
            var response = Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("GroupName", groupName);
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            response = Browser.Delete($"/groups/{groupName}", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            response = Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("GroupName", groupName);
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
    }
}