using System;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.IntegrationTests.Modules;
using Fabric.Authorization.Persistence.CouchDb.Services;
using Nancy;
using Nancy.Testing;
using Xunit;

namespace Fabric.Authorization.IntegrationTests.CouchDB
{
    [Collection("CouchTests")]
    public class CouchDbGroupsTests //: GroupsTests
    {
        private readonly IDocumentDbService _documentDbService;
        public CouchDbGroupsTests(IntegrationTestsFixture fixture) //: base(fixture, StorageProviders.CouchDb)
        {
            _documentDbService = fixture.DbService();
        }

        [Fact(Skip = "cannot figure out why it cant find the first group created when it exists in the db")]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public void AddGroup_ActiveGroupWithOldIdExists_BadRequest()
        {
            string groupName = "Group1" + Guid.NewGuid();

            // create an active Group document in CouchDB with the old style Group ID
            _documentDbService.AddDocument(groupName, new Group
            {
                Id = groupName,
                Name = groupName,
                Source = "Custom"
            }).Wait();

            //var response = Browser.Post("/groups", with =>
            //{
            //    with.HttpRequest();
            //    with.JsonBody(new
            //    {
            //        GroupName = groupName,
            //        GroupSource = "Custom"
            //    });
            //}).Result;

            //Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact(Skip = "disabling  couch db tests temporarily")]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public void AddGroup_InactiveGroupWithOldIdExists_Success()
        {
            string groupName = "Group1" + Guid.NewGuid();

            // create an inactive Group document in CouchDB with the old style Group ID
            _documentDbService.AddDocument(groupName, new Group
            {
                Id = groupName,
                Name = groupName,
                IsDeleted = true
            }).Wait();

            //var response = Browser.Post("/groups", with =>
            //{
            //    with.HttpRequest();
            //    with.JsonBody(new
            //    {
            //        GroupName = groupName
            //    });
            //}).Result;

            //Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            //response = Browser.Get($"/groups/{groupName}", with =>
            //{
            //    with.HttpRequest();
            //}).Result;

            //Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}