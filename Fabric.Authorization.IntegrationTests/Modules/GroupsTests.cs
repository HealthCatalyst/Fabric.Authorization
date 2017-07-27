using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Modules;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Domain.Stores.CouchDB;
using Fabric.Authorization.Domain.Stores.Services;
using Nancy;
using Nancy.Testing;
using Xunit;

namespace Fabric.Authorization.IntegrationTests
{
    [Collection("InMemoryTests")]
    public class GroupsTests : IntegrationTestsFixture
    {
        public GroupsTests(bool useInMemoryDB = true)
        {
            var store = useInMemoryDB ? new InMemoryGroupStore() : (IGroupStore)new CouchDbGroupStore(this.DbService(), this.Logger);
            var roleStore = useInMemoryDB
                ? new InMemoryRoleStore()
                : (IRoleStore) new CouchDbRoleStore(this.DbService(), this.Logger);
            var permissionStore = useInMemoryDB
                ? new InMemoryPermissionStore()
                : (IPermissionStore) new CouchDbPermissionStore(this.DbService(), this.Logger);
            var groupService = new GroupService(store, roleStore, permissionStore, new RoleService(roleStore, permissionStore));

            this.Browser = new Browser(with =>
            {
                with.Module(new GroupsModule(
                        groupService,
                        new Domain.Validators.GroupValidator(groupService),
                        this.Logger));
                with.RequestStartup((_, __, context) =>
                {
                    context.CurrentUser = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>()
                    {
                        new Claim(Claims.Scope, Scopes.ManageClientsScope),
                        new Claim(Claims.Scope, Scopes.ReadScope),
                        new Claim(Claims.Scope, Scopes.WriteScope),
                    }, "testprincipal"));
                });
            });
            
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
                with.FormValue("Id", groupName);
                with.FormValue("GroupName", groupName);
            }).Result;

            var getResponse = this.Browser.Get($"/groups/{groupName}", with =>
                {
                    with.HttpRequest();
                    with.Header("Accept", "application/json");
                }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.True(getResponse.Body.AsString().Contains(groupName));
        }

        [Theory]
        [InlineData("BatchGroup1")]
        [InlineData("BatchGroup2")]
        [InlineData("6AC32A47-36C1-23BF-AA22-6C1028AA5DC3")]
        public void TestAddNewGroupBatch_Success(string groupName)
        {
            var postResponse = this.Browser.Post("/groups/UpdateGroups", with =>
            {
                with.HttpRequest();
                with.FormValue("Id[0]", groupName + "_0");
                with.FormValue("Id[1]", groupName + "_1");
                with.FormValue("Id[2]", groupName + "_2");

                with.FormValue("GroupName[0]", groupName + "_0");
                with.FormValue("GroupName[1]", groupName + "_1");
                with.FormValue("GroupName[2]", groupName + "_2");
            }).Result;

            var getResponse0 = this.Browser.Get($"/groups/{groupName}_0", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            var getResponse1 = this.Browser.Get($"/groups/{groupName}_1", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            var getResponse2 = this.Browser.Get($"/groups/{groupName}_2", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.NoContent, postResponse.StatusCode);

            Assert.Equal(HttpStatusCode.OK, getResponse0.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse1.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse2.StatusCode);

            Assert.True(getResponse0.Body.AsString().Contains(groupName + "_0"));
            Assert.True(getResponse1.Body.AsString().Contains(groupName + "_1"));
            Assert.True(getResponse2.Body.AsString().Contains(groupName + "_2"));
        }

        [Theory]
        [InlineData("BatchUpdateGroup1")]
        [InlineData("BatchUpdateGroup2")]
        public void TestUpdateGroupBatch_Success(string groupName)
        {
            var postResponse = this.Browser.Post("/groups/UpdateGroups", with =>
            {
                with.HttpRequest();
                with.FormValue("Id[0]", groupName + "_0");
                with.FormValue("Id[1]", groupName + "_1");
                with.FormValue("Id[2]", groupName + "_2");
                with.FormValue("GroupName[0]", groupName + "_0");
                with.FormValue("GroupName[1]", groupName + "_1");
                with.FormValue("GroupName[2]", groupName + "_2");
            }).Result;

            Assert.Equal(HttpStatusCode.NoContent, postResponse.StatusCode);

            // Replace groups. _0 should be removed and _3 should be added.
            postResponse = this.Browser.Post("/groups/UpdateGroups", with =>
            {
                with.HttpRequest();
                with.FormValue("Id[0]", groupName + "_1");
                with.FormValue("Id[1]", groupName + "_2");
                with.FormValue("Id[2]", groupName + "_3");
                with.FormValue("GroupName[0]", groupName + "_1");
                with.FormValue("GroupName[1]", groupName + "_2");
                with.FormValue("GroupName[2]", groupName + "_3");
            }).Result;

            Assert.Equal(HttpStatusCode.NoContent, postResponse.StatusCode);

            var getResponse0 = this.Browser.Get($"/groups/{groupName}_0", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            var getResponse1 = this.Browser.Get($"/groups/{groupName}_1", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            var getResponse2 = this.Browser.Get($"/groups/{groupName}_2", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            var getResponse3 = this.Browser.Get($"/groups/{groupName}_3", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.NotFound, getResponse0.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse1.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse2.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse3.StatusCode);

            Assert.True(getResponse1.Body.AsString().Contains(groupName + "_1"));
            Assert.True(getResponse2.Body.AsString().Contains(groupName + "_2"));
            Assert.True(getResponse3.Body.AsString().Contains(groupName + "_3"));
        }

        [Theory]
        [InlineData("RepeatedGroup1")]
        [InlineData("RepeatedGroup2")]
        public void TestAddNewGroup_Fail(string groupName)
        {
            this.Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("Id", groupName);
                with.FormValue("GroupName", groupName);
            }).Wait();

            // Repeat
            var postResponse = this.Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("Id", groupName);
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
                with.FormValue("Id", groupName);
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