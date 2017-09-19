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

            Fixture.InitializeIdentitySearchBrowser(mockIdentityServiceProvider.Object);

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
        public readonly string AtlasClientId = "atlas";
        public readonly string AdminAtlasGroupName = "adminAtlasGroup";
        public readonly string UserAtlasGroupName = "userAtlasGroup";
        public readonly string AdminAtlasRoleName = "adminAtlasRole";
        public readonly string UserAtlasRoleName = "userAtlasRole";

        private ClientService _clientService;
        private RoleService _roleService;
        private GroupService _groupService;

        private bool _isInitialized;

        public void Initialize(bool useInMemoryDb)
        {
            if (_isInitialized)
            {
                return;
            }

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
            _roleService = new RoleService(roleStore, new InMemoryPermissionStore(), _clientService);

            Browser = new Browser(with =>
            {
                with.Module(new GroupsModule(
                    _groupService,
                    new GroupValidator(_groupService),
                    Logger,
                    DefaultPropertySettings));

                with.Module(new RolesModule(
                    _roleService,
                    _clientService,
                    new RoleValidator(_roleService),
                    Logger));

                with.Module(new ClientsModule(
                    _clientService,
                    new ClientValidator(_clientService),
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

            InitializeData();
            _isInitialized = true;
        }

        public void InitializeIdentitySearchBrowser(IIdentityServiceProvider identityServiceProvider)
        {
            Browser = new Browser(with =>
            {
                with.FieldNameConverter<UnderscoredFieldNameConverter>();

                with.Module(new IdentitySearchModule(
                    new IdentitySearchService(_clientService, _roleService, _groupService, identityServiceProvider), 
                    new IdentitySearchRequestValidator(), 
                    Logger));

                with.RequestStartup((_, pipelines, context) =>
                {
                    context.CurrentUser = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                    {
                        new Claim(Claims.Scope, Scopes.ReadScope),
                        new Claim(Claims.Scope, IdentityScopes.ReadScope),
                        new Claim(Claims.ClientId, AtlasClientId)
                    }, "rolesprincipal"));
                    pipelines.BeforeRequest += ctx => RequestHooks.SetDefaultVersionInUrl(ctx);
                });
            }, withDefaults => withDefaults.HostName("testhost"));
        }

        private void InitializeData()
        {

            // create the Atlas client
            Browser.Post("/clients", with =>
            {
                with.HttpRequest();
                with.FormValue("Id", AtlasClientId);
                with.FormValue("Name", AtlasClientId);
                with.Header("Accept", "application/json");
            }).Wait();

            // create the 

            // create roles
            var userAtlasRoleResponse = Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", AtlasClientId);
                with.FormValue("Name", UserAtlasRoleName);
            }).Result;

            var adminAtlasRoleResponse = Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", AtlasClientId);
                with.FormValue("Name", AdminAtlasRoleName);
            }).Result;

            // create groups
            Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("GroupName", UserAtlasGroupName);
                with.FormValue("GroupSource", "Custom");
                with.Header("Accept", "application/json");
            }).Wait();

            Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("GroupName", AdminAtlasGroupName);
                with.FormValue("GroupSource", "Windows");
                with.Header("Accept", "application/json");
            }).Wait();

            // add role to group
            Browser.Post($"/groups/{AdminAtlasGroupName}/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Id", adminAtlasRoleResponse.Body.DeserializeJson<RoleApiModel>().Id.ToString());
            }).Wait();

            Browser.Post($"/groups/{UserAtlasGroupName}/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Id", userAtlasRoleResponse.Body.DeserializeJson<RoleApiModel>().Id.ToString());
            }).Wait();

            // add user to custom group
            Browser.Post($"/groups/{UserAtlasGroupName}/users", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("SubjectId", "atlas_user");
            });
        }
    }
}