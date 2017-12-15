using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.IntegrationTests.Modules;
using Nancy;
using Nancy.Testing;
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
                Name = groupName,
                Source = "Custom"
            }).Wait();

            var response = Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = groupName,
                    GroupSource = "Custom"
                });
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
                with.JsonBody(new
                {
                    GroupName = groupName
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            response = Browser.Get($"/groups/{groupName}", with =>
            {
                with.HttpRequest();
            }).Result;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}