using Fabric.Authorization.Domain.Models;
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
        public void AddGroup_ActiveGroupWithOldIdExists_BadRequest()
        {
            const string groupName = "Group1";
            
            // create an active Group document in CouchDB with the old style Group ID
            DbService().AddDocument("group1", new Group
            {
                Id = groupName,
                Name = groupName
            }).Wait();

            var response = Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("GroupName", groupName);
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        [DisplayTestMethodName]
        public void AddGroup_InactiveGroupWithOldIdExists_Success()
        {
            const string groupName = "Group1";

            // create an inactive Group document in CouchDB with the old style Group ID
            DbService().AddDocument("group1", new Group
            {
                Id = groupName,
                Name = groupName,
                IsDeleted = true
            }).Wait();

            var response = Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("GroupName", groupName);
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            response = Browser.Get($"/groups/{groupName}", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}