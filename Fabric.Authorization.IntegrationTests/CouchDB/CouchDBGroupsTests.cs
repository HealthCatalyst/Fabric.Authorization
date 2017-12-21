using System;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.IntegrationTests.Modules;
using Nancy;
using Nancy.Testing;
using Xunit;

namespace Fabric.Authorization.IntegrationTests.CouchDB
{
    [Collection("CouchTests")]
    public class CouchDbGroupsTests : GroupsTests
    {
        private readonly IDocumentDbService _documentDbService;
        private readonly Browser _browser;
        public CouchDbGroupsTests(IntegrationTestsFixture fixture) : base(fixture, false)
        {
            _documentDbService = fixture.DbService();
            _browser = fixture.GetBrowser(Principal, false);
        }

        
        [Fact(Skip="cause")]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public void AddGroup_ActiveGroupWithOldIdExists_BadRequest()
        {
            string groupName = "Group1" + Guid.NewGuid();

            // create an active Group document in CouchDB with the old style Group ID
            _documentDbService.AddDocument("group1", new Group
            {
                Id = groupName,
                Name = groupName,
                Source = "Custom"
            }).Wait();

            var response = _browser.Post("/groups", with =>
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

        [Fact(Skip = "cause")]
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

            var response = _browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = groupName
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            response = _browser.Get($"/groups/{groupName}", with =>
            {
                with.HttpRequest();
            }).Result;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}