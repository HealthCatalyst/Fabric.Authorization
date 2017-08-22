using System.Collections.Generic;
using System.Security.Claims;
using Fabric.Authorization.API;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Modules;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Domain.Stores.CouchDB;
using Fabric.Authorization.Domain.Stores.Services;
using Fabric.Authorization.Domain.Validators;
using Nancy;
using Nancy.Testing;
using Xunit;

namespace Fabric.Authorization.IntegrationTests.Modules
{
    [Collection("InMemoryTests")]
    public class GroupsTests : IntegrationTestsFixture
    {
        public GroupsTests(bool useInMemoryDB = true)
        {
            var store = useInMemoryDB
                ? new InMemoryGroupStore()
                : (IGroupStore) new CouchDbGroupStore(DbService(), Logger, EventContextResolverService);
            var roleStore = useInMemoryDB
                ? new InMemoryRoleStore()
                : (IRoleStore) new CouchDbRoleStore(DbService(), Logger, EventContextResolverService);
            var permissionStore = useInMemoryDB
                ? new InMemoryPermissionStore()
                : (IPermissionStore) new CouchDbPermissionStore(DbService(), Logger, EventContextResolverService);
            var groupService = new GroupService(store, roleStore, new RoleService(roleStore, permissionStore));

            Browser = new Browser(with =>
            {
                with.Module(new GroupsModule(
                    groupService,
                    new GroupValidator(groupService),
                    Logger));
                with.RequestStartup((_, pipelines, context) =>
                {
                    context.CurrentUser = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                    {
                        new Claim(Claims.Scope, Scopes.ManageClientsScope),
                        new Claim(Claims.Scope, Scopes.ReadScope),
                        new Claim(Claims.Scope, Scopes.WriteScope)
                    }, "testprincipal"));
                    pipelines.BeforeRequest += ctx => RequestHooks.SetDefaultVersionInUrl(ctx);
                });
            }, withDefaults => withDefaults.HostName("testhost"));
        }

        [Theory]
        [DisplayTestMethodName]
        [InlineData("NonexistentGroup")]
        [InlineData("NonexistentGroup2")]
        public void GetGroup_NonexistentGroup_Fail(string groupName)
        {
            var get = Browser.Get($"/groups/{groupName}", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        }

        [Theory]
        [DisplayTestMethodName]
        [InlineData("Group1", "Source1")]
        [InlineData("Group2", "Source2")]
        [InlineData("6BC32347-36A1-44CF-AA0E-6C1038AA1DF3", "Source3")]
        public void AddGroup_SingleGroup_Success(string groupName, string groupSource)
        {
            var postResponse = Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("Id", groupName);
                with.FormValue("GroupName", groupName);
                with.FormValue("GroupSource", groupSource);
                with.Header("Accept", "application/json");
            }).Result;

            var getResponse = Browser.Get($"/groups/{groupName}", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.True(getResponse.Body.AsString().Contains(groupName));
        }

        [Theory]
        [DisplayTestMethodName]
        [InlineData("BatchGroup1", "BatchSource1")]
        [InlineData("BatchGroup2", "BatchSource2")]
        [InlineData("6AC32A47-36C1-23BF-AA22-6C1028AA5DC3", "BatchSource3")]
        public void AddGroup_Batch_Success(string groupName, string groupSource)
        {
            var postResponse = Browser.Post("/groups/UpdateGroups", with =>
            {
                with.HttpRequest();
                with.FormValue("Id[0]", groupName + "_0");
                with.FormValue("Id[1]", groupName + "_1");
                with.FormValue("Id[2]", groupName + "_2");

                with.FormValue("GroupName[0]", groupName + "_0");
                with.FormValue("GroupName[1]", groupName + "_1");
                with.FormValue("GroupName[2]", groupName + "_2");

                with.FormValue("GroupSource[0]", groupSource + "_0");
                with.FormValue("GroupSource[1]", groupSource + "_1");
                with.FormValue("GroupSource[2]", groupSource + "_2");

                with.Header("Accept", "application/json");
            }).Result;

            var getResponse0 = Browser.Get($"/groups/{groupName}_0", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            var getResponse1 = Browser.Get($"/groups/{groupName}_1", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            var getResponse2 = Browser.Get($"/groups/{groupName}_2", with =>
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

            Assert.True(getResponse0.Body.AsString().Contains(groupSource + "_0"));
            Assert.True(getResponse1.Body.AsString().Contains(groupSource + "_1"));
            Assert.True(getResponse2.Body.AsString().Contains(groupSource + "_2"));
        }

        [Theory]
        [DisplayTestMethodName]
        [InlineData("", "Source1")]
        [InlineData(null, "Source2")]
        public void AddGroup_NullOrEmptyName_BadRequest(string groupName, string groupSource)
        {
            var postResponse = Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("Id", groupName);
                with.FormValue("GroupName", groupName);
                with.FormValue("GroupSource", groupSource);
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.BadRequest, postResponse.StatusCode);
        }

        [Theory]
        [DisplayTestMethodName]
        [InlineData("Source1")]
        public void AddGroup_MissingName_BadRequest(string groupSource)
        {
            var postResponse = Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("Id", "GroupId");
                with.FormValue("GroupSource", groupSource);
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.BadRequest, postResponse.StatusCode);
        }

        [Theory]
        [DisplayTestMethodName]
        [InlineData("Source1", "")]
        [InlineData("Source2", null)]
        public void AddGroup_NullOrEmptySource_BadRequest(string groupName, string groupSource)
        {
            var postResponse = Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("Id", groupName);
                with.FormValue("GroupName", groupName);
                with.FormValue("GroupSource", groupSource);
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.BadRequest, postResponse.StatusCode);
        }

        [Theory]
        [DisplayTestMethodName]
        [InlineData("Name1")]
        public void AddGroup_MissingSource_BadRequest(string groupName)
        {
            var postResponse = Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("Id", groupName);
                with.FormValue("GroupName", groupName);
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.BadRequest, postResponse.StatusCode);
        }

        [Theory]
        [DisplayTestMethodName]
        [InlineData("RepeatedGroup1")]
        [InlineData("RepeatedGroup2")]
        public void AddGroup_AlreadyExists_Fail(string groupName)
        {
            Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("Id", groupName);
                with.FormValue("GroupName", groupName);
                with.Header("Accept", "application/json");
            }).Wait();

            // Repeat
            var postResponse = Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("Id", groupName);
                with.FormValue("GroupName", groupName);
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.BadRequest, postResponse.StatusCode);
        }

        [Theory]
        [DisplayTestMethodName]
        [InlineData("BatchUpdateGroup1", "BatchUpdateSource1")]
        [InlineData("BatchUpdateGroup2", "BatchUpdateSource2")]
        public void UpdateGroup_Batch_Success(string groupName, string groupSource)
        {
            var postResponse = Browser.Post("/groups/UpdateGroups", with =>
            {
                with.HttpRequest();
                with.FormValue("Id[0]", groupName + "_0");
                with.FormValue("Id[1]", groupName + "_1");
                with.FormValue("Id[2]", groupName + "_2");

                with.FormValue("GroupName[0]", groupName + "_0");
                with.FormValue("GroupName[1]", groupName + "_1");
                with.FormValue("GroupName[2]", groupName + "_2");

                with.FormValue("GroupSource[0]", groupSource + "_0");
                with.FormValue("GroupSource[1]", groupSource + "_1");
                with.FormValue("GroupSource[2]", groupSource + "_2");

                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.NoContent, postResponse.StatusCode);

            // Replace groups. _0 should be removed and _3 should be added.
            postResponse = Browser.Post("/groups/UpdateGroups", with =>
            {
                with.HttpRequest();
                with.FormValue("Id[0]", groupName + "_1");
                with.FormValue("Id[1]", groupName + "_2");
                with.FormValue("Id[2]", groupName + "_3");
                with.FormValue("GroupName[0]", groupName + "_1");
                with.FormValue("GroupName[1]", groupName + "_2");
                with.FormValue("GroupName[2]", groupName + "_3");
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.NoContent, postResponse.StatusCode);

            var getResponse0 = Browser.Get($"/groups/{groupName}_0", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            var getResponse1 = Browser.Get($"/groups/{groupName}_1", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            var getResponse2 = Browser.Get($"/groups/{groupName}_2", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            var getResponse3 = Browser.Get($"/groups/{groupName}_3", with =>
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
        [DisplayTestMethodName]
        [InlineData("GroupToBeDeleted", "Source1")]
        [InlineData("GroupToBeDeleted2", "Source2")]
        public void DeleteGroup_SingleGroup_Success(string groupName, string groupSource)
        {
            Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("Id", groupName);
                with.FormValue("GroupName", groupName);
                with.FormValue("GroupSource", groupSource);
                with.Header("Accept", "application/json");
            }).Wait();

            var delete = Browser.Delete($"/groups/{groupName}", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        }

        [Theory]
        [DisplayTestMethodName]
        [InlineData("NonexistentGroup")]
        [InlineData("NonexistentGroup2")]
        public void DeleteGroup_NonExistentGroup_Fail(string groupName)
        {
            var delete = Browser.Delete($"/groups/{groupName}", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        }
    }
}