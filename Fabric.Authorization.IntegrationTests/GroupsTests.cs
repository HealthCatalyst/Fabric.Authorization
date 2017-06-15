using System;
using Fabric.Authorization.API.Modules;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores;
using Nancy;
using Nancy.Testing;
using Xunit;

namespace Fabric.Authorization.IntegrationTests
{
    public class GroupsTests : IntegrationTestsFixture
    {
        public GroupsTests()
        {
            var groupService = new GroupService(new InMemoryGroupStore(), new InMemoryRoleStore());
            this.Browser = new Browser(with => with.Module(new GroupsModule(groupService)));
        }

        [Theory]
        [InlineData("InexistentGroup")]
        [InlineData("InexistentGroup2")]
        public void TestGetGroup_Fail(string groupName)
        {
            var get = this.Browser.Get($"/groups/{groupName}", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        }

        [Theory]
        [InlineData("Group1")]
        [InlineData("Group2")]
        [InlineData("6BC32347-36A1-44CF-AA0E-6C1038AA1DF3")]
        public void TestAddNewGroup_Success(string groupName)
        {
            var postResponse = this.Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("GroupName", groupName);
            }).Result;

            var getResponse = this.Browser.Get($"/groups/{groupName}", with =>
                {
                    with.HttpRequest();
                    with.Header("Accept", "application/json");
                }).Result;

            Assert.Equal(HttpStatusCode.NoContent, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.True(getResponse.Body.AsString().Contains(groupName));
        }

        [Theory]
        [InlineData("RepeatedGroup1")]
        [InlineData("RepeatedGroup2")]
        public void TestAddNewGroup_Fail(string groupName)
        {
            this.Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("GroupName", groupName);
            }).Wait();

            // Repeat
            var postResponse = this.Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("GroupName", groupName);
            }).Result;

            Assert.Equal(HttpStatusCode.BadRequest, postResponse.StatusCode);
        }

        [Theory]
        [InlineData("GroupToBeDeleted")]
        [InlineData("GroupToBeDeleted2")]
        public void TestDeleteGroup_Success(string groupName)
        {
            this.Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("GroupName", groupName);
            }).Wait();

            var delete = this.Browser.Delete($"/groups/{groupName}", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        }

        [Theory]
        [InlineData("InexistentGroup")]
        [InlineData("InexistentGroup2")]
        public void TestDeleteGroup_Fail(string groupName)
        {
            var delete = this.Browser.Delete($"/groups/{groupName}", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        }
    }
}