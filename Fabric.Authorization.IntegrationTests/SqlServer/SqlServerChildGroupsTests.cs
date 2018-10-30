using Catalyst.Fabric.Authorization.Models;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.IntegrationTests.Modules;
using Nancy;
using Nancy.Testing;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Fabric.Authorization.IntegrationTests.SqlServer
{
    [Collection("SqlServerTests")]
    public class SqlServerChildGroupsTests : ChildGroupsTests
    {
        public SqlServerChildGroupsTests(IntegrationTestsFixture fixture, SqlServerIntegrationTestsFixture sqlFixture) : base(fixture, StorageProviders.SqlServer, sqlFixture.ConnectionStrings)
        {
        }

        [Fact]
        public async Task AddChildGroup_ExistingChildGroupWithDifferentCase_SuccessAsync()
        {
            var parentGroup = await SetupGroupAsync(Guid.NewGuid().ToString(), GroupConstants.CustomSource, "Custom Parent Group", "Custom Parent Group");
            var childGroup1 = await SetupGroupAsync("Child Group 1" + Guid.NewGuid(), GroupConstants.DirectorySource, "Child Group 1", "Child Group 1");

            var lowerGroupName = childGroup1.GroupName.ToLower();

            var postResponse = await _browser.Post($"/groups/{parentGroup.GroupName}/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new { GroupName = lowerGroupName }
                });
            });

            Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

            var groupApiModel = JsonConvert.DeserializeObject<GroupRoleApiModel>(postResponse.Body.AsString());
            Assert.NotNull(groupApiModel);

            Assert.Single(groupApiModel.Children);
            Assert.Contains(groupApiModel.Children, c => c.GroupName == childGroup1.GroupName);
            Assert.DoesNotContain(groupApiModel.Children, c => c.GroupName == lowerGroupName);
        }
    }
}
