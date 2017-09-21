using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Fabric.Authorization.API;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Converters;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.API.Models.Search;
using Fabric.Authorization.API.Models.Search.Validators;
using Fabric.Authorization.API.Modules;
using Fabric.Authorization.API.RemoteServices.Identity.Models;
using Fabric.Authorization.API.RemoteServices.Identity.Providers;
using Fabric.Authorization.API.Services;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Domain.Stores.CouchDB;
using Fabric.Authorization.Domain.Stores.InMemory;
using Fabric.Authorization.Domain.Stores.Services;
using Fabric.Authorization.Domain.Validators;
using Moq;
using Nancy;
using Nancy.Testing;
using Xunit;

namespace Fabric.Authorization.IntegrationTests.Modules
{
    [Collection("InMemoryTests")]
    public class IdentitySearchTests : IClassFixture<IdentitySearchFixture>
    {
        protected readonly IdentitySearchFixture Fixture;

        public IdentitySearchTests(IdentitySearchFixture fixture)
        {
            Fixture = fixture;
            Fixture.Initialize(true);
        }

        [Fact]
        public void IdentitySearch_ClientIdDoesNotExist_NotFoundException()
        {
            Fixture.InitializeBrowser(new Mock<IIdentityServiceProvider>().Object);

            var result = Fixture.Browser.Get(
                "/search/identities", with =>
                {
                    with.HttpRequest();
                    with.Header("Accept", "application/json");
                    with.Query("client_id", "blah");
                    with.Query("sort_key", "name");
                    with.Query("sort_dir", "desc");
                    with.Query("filter", "brian");
                    with.Query("page_number", "1");
                    with.Query("page_size", "1");
                }).Result;

            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        }

        [Fact]
        public void IdentitySearch_ClientWithoutRoles_EmptyResponse()
        {
            var mockIdentityServiceProvider = new Mock<IIdentityServiceProvider>();
            Fixture.InitializeClientWithoutRoles(mockIdentityServiceProvider.Object);

            var response = Fixture.Browser.Get(
                "/search/identities", with =>
                {
                    with.HttpRequest();
                    with.Header("Accept", "application/json");
                    with.Query("client_id", Fixture.AtlasClientId);
                    with.Query("sort_key", "name");
                    with.Query("sort_dir", "desc");
                    with.Query("filter", "brian");
                    with.Query("page_number", "1");
                    with.Query("page_size", "1");
                }).Result;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var results = response.Body.DeserializeJson<List<IdentitySearchResponse>>();
            Assert.Equal(0, results.Count);
        }

        [Fact]
        public void IdentitySearch_ClientWithRolesAndNoGroups_EmptyResponse()
        {
            var mockIdentityServiceProvider = new Mock<IIdentityServiceProvider>();
            Fixture.InitializeClientWithRolesAndNoGroups(mockIdentityServiceProvider.Object);

            var response = Fixture.Browser.Get(
                "/search/identities", with =>
                {
                    with.HttpRequest();
                    with.Header("Accept", "application/json");
                    with.Query("client_id", Fixture.AtlasClientId);
                    with.Query("sort_key", "name");
                    with.Query("sort_dir", "desc");
                    with.Query("filter", "brian");
                    with.Query("page_number", "1");
                    with.Query("page_size", "1");
                }).Result;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var results = response.Body.DeserializeJson<List<IdentitySearchResponse>>();
            Assert.Equal(0, results.Count);
        }

        [Fact]
        public void IdentitySearch_ValidRequest_Success()
        {
            var lastLoginDate = new DateTime(2017, 9, 15).ToUniversalTime();

            var mockIdentityServiceProvider = new Mock<IIdentityServiceProvider>();
            mockIdentityServiceProvider
                .Setup(m => m.Search(Fixture.AtlasClientId, new List<string> {"atlas_user"}))
                .ReturnsAsync(() => new List<UserSearchResponse>
                {
                    new UserSearchResponse
                    {
                        SubjectId = "atlas_user",
                        FirstName = "Robert",
                        MiddleName = "Brian",
                        LastName = "Smith",
                        LastLoginDate = lastLoginDate
                    }
                });

            Fixture.InitializeSuccessData(mockIdentityServiceProvider.Object);

            var response = Fixture.Browser.Get(
                "/search/identities", with =>
                {
                    with.HttpRequest();
                    with.Header("Accept", "application/json");
                    with.Query("client_id", Fixture.AtlasClientId);
                    with.Query("sort_key", "name");
                    with.Query("sort_dir", "desc");
                    with.Query("filter", "brian");
                    with.Query("page_number", "1");
                    with.Query("page_size", "1");
                }).Result;

            var results = response.Body.DeserializeJson<List<IdentitySearchResponse>>();

            Assert.Equal(1, results.Count);

            var result1 = results[0];
            Assert.Equal("atlas_user", result1.SubjectId);
            Assert.Equal("Robert", result1.FirstName);
            Assert.Equal("Brian", result1.MiddleName);
            Assert.Equal("Smith", result1.LastName);
            Assert.NotNull(result1.LastLogin);
            Assert.Equal(lastLoginDate, result1.LastLogin.Value.ToUniversalTime());
            Assert.Equal(Fixture.UserAtlasRoleName, result1.Roles.FirstOrDefault());
        }
    }

    public class IdentitySearchFixture : IntegrationTestsFixture
    {
        public string AtlasClientId { get; private set; }
        public string AdminAtlasGroupName { get; private set; }
        public string UserAtlasGroupName { get; private set; }
        public string AdminAtlasRoleName { get; private set; }
        public string UserAtlasRoleName { get; private set; }

        private ClientService _clientService;
        private GroupService _groupService;

        public RoleService RoleService { get; private set; }

        public void Initialize(bool useInMemoryDb)
        {
            var groupStore = useInMemoryDb
                ? new InMemoryGroupStore()
                : (IGroupStore)new CouchDbGroupStore(DbService(), Logger, EventContextResolverService);

            var roleStore = useInMemoryDb
                ? new InMemoryRoleStore()
                : (IRoleStore)new CouchDbRoleStore(DbService(), Logger, EventContextResolverService);

            var userStore = useInMemoryDb
                ? new InMemoryUserStore()
                : (IUserStore)new CouchDbUserStore(DbService(), Logger, EventContextResolverService);

            var clientStore = useInMemoryDb
                ? new InMemoryClientStore()
                : (IClientStore)new CouchDbClientStore(DbService(), Logger, EventContextResolverService);

            _groupService = new GroupService(groupStore, roleStore, userStore);
            _clientService = new ClientService(clientStore);
            RoleService = new RoleService(roleStore, new InMemoryPermissionStore(), _clientService);

            AtlasClientId = $"atlas-{DateTime.Now.Ticks}";
            AdminAtlasGroupName = $"adminAtlasGroup-{DateTime.Now.Ticks}";
            UserAtlasGroupName = $"userAtlasGroup-{DateTime.Now.Ticks}";
            AdminAtlasRoleName = $"adminAtlasRole-{DateTime.Now.Ticks}";
            UserAtlasRoleName = $"userAtlasRole-{DateTime.Now.Ticks}";
        }

        public void InitializeBrowser(IIdentityServiceProvider identityServiceProvider)
        {
            Browser = new Browser(with =>
            {
                // TODO: move this to base class and refactor all integration tests to use
                with.FieldNameConverter<UnderscoredFieldNameConverter>();

                with.Module(new GroupsModule(
                    _groupService,
                    new GroupValidator(_groupService),
                    Logger,
                    DefaultPropertySettings));

                with.Module(new RolesModule(
                    RoleService,
                    _clientService,
                    new RoleValidator(RoleService),
                    Logger));

                with.Module(new ClientsModule(
                    _clientService,
                    new ClientValidator(_clientService),
                    Logger));

                with.Module(new IdentitySearchModule(
                    new IdentitySearchService(_clientService, RoleService, _groupService, identityServiceProvider),
                    new IdentitySearchRequestValidator(),
                    Logger));

                with.RequestStartup((_, pipelines, context) =>
                {
                    context.CurrentUser = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                    {
                        new Claim(Claims.Scope, Scopes.ManageClientsScope),
                        new Claim(Claims.Scope, Scopes.ReadScope),
                        new Claim(Claims.Scope, Scopes.WriteScope),
                        new Claim(Claims.ClientId, AtlasClientId)
                    }, "rolesprincipal"));
                    pipelines.BeforeRequest += ctx => RequestHooks.SetDefaultVersionInUrl(ctx);
                });
            }, withDefaults => withDefaults.HostName("testhost"));
        }

        public void InitializeSuccessData(IIdentityServiceProvider identityServiceProvider)
        {
            InitializeBrowser(identityServiceProvider);

            // create the Atlas client
            var response = Browser.Post("/clients", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Id", AtlasClientId);
                with.FormValue("Name", AtlasClientId);
            }).Result;

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // create roles
            var userAtlasRoleResponse = Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", AtlasClientId);
                with.FormValue("Name", UserAtlasRoleName);
            }).Result;

            Assert.Equal(HttpStatusCode.Created, userAtlasRoleResponse.StatusCode);

            var adminAtlasRoleResponse = Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", AtlasClientId);
                with.FormValue("Name", AdminAtlasRoleName);
            }).Result;

            Assert.Equal(HttpStatusCode.Created, adminAtlasRoleResponse.StatusCode);

            // create groups
            response = Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("GroupName", UserAtlasGroupName);
                with.FormValue("GroupSource", "Custom");
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            response = Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("GroupName", AdminAtlasGroupName);
                with.FormValue("GroupSource", "Windows");
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // add role to group
            response = Browser.Post($"/groups/{AdminAtlasGroupName}/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Id", adminAtlasRoleResponse.Body.DeserializeJson<RoleApiModel>().Id.ToString());
            }).Result;

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            response = Browser.Post($"/groups/{UserAtlasGroupName}/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Id", userAtlasRoleResponse.Body.DeserializeJson<RoleApiModel>().Id.ToString());
            }).Result;

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // add user to custom group
            response = Browser.Post($"/groups/{UserAtlasGroupName}/users", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("SubjectId", "atlas_user");
            }).Result;

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        public void InitializeClientWithoutRoles(IIdentityServiceProvider identityServiceProvider)
        {
            InitializeBrowser(identityServiceProvider);

            // create the Atlas client
            var response = Browser.Post("/clients", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Id", AtlasClientId);
                with.FormValue("Name", AtlasClientId);
            }).Result;

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        public void InitializeClientWithRolesAndNoGroups(IIdentityServiceProvider identityServiceProvider)
        {
            InitializeBrowser(identityServiceProvider);

            // create the Atlas client
            var response = Browser.Post("/clients", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Id", AtlasClientId);
                with.FormValue("Name", AtlasClientId);
            }).Result;

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // create roles
            response = Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", AtlasClientId);
                with.FormValue("Name", UserAtlasRoleName);
            }).Result;

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            response = Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", AtlasClientId);
                with.FormValue("Name", AdminAtlasRoleName);
            }).Result;

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
    }
}