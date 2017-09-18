using System;
using System.Collections.Generic;
using System.Security.Claims;
using Fabric.Authorization.API;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Modules;
using Fabric.Authorization.API.RemoteServices.Identity.Models;
using Fabric.Authorization.API.RemoteServices.Identity.Providers;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Domain.Stores.CouchDB;
using Fabric.Authorization.Domain.Stores.InMemory;
using Fabric.Authorization.Domain.Stores.Services;
using Fabric.Authorization.Domain.Validators;
using Moq;
using Nancy.Testing;
using Xunit;

namespace Fabric.Authorization.IntegrationTests.Modules
{
    [Collection("InMemoryTests")]
    public class IdentitySearchTests : IClassFixture<IdentitySearchFixture>
    {
        private readonly IdentitySearchFixture _fixture;

        public IdentitySearchTests(IdentitySearchFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void IdentitySearch_InvalidRequest_BadRequestException()
        {
            
        }

        [Fact]
        public void IdentitySearch_UnauthorizedUser_ForbiddenException()
        {

        }

        [Fact]
        public void IdentitySearch_ValidRequest_Success()
        {
            var lastLoginDate = new DateTime(2017, 9, 15);

            var mockIdentityServiceProvider = new Mock<IIdentityServiceProvider>();
            mockIdentityServiceProvider
                .Setup(m => m.Search(_fixture.AtlasClientId, new List<string> { "atlas_user" }))
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
        }
    }

    public class IdentitySearchFixture : IntegrationTestsFixture
    {
        public readonly string AtlasClientId = "atlas";
        public readonly string AdminAtlasGroupName = "adminAtlasGroup";
        public readonly string UserAtlasGroupName = "userAtlasGroup";
        public readonly string AdminAtlasRoleName = "adminAtlasRole";
        public readonly string UserAtlasRoleName = "userAtlasRole";

        private readonly ClientService _clientService;
        private readonly RoleService _roleService;
        private readonly GroupService _groupService;

        public IdentitySearchFixture(bool useInMemoryDB = true)
        {
            var groupStore = useInMemoryDB
                ? new InMemoryGroupStore()
                : (IGroupStore)new CouchDbGroupStore(DbService(), Logger, EventContextResolverService);

            var roleStore = useInMemoryDB
                ? new InMemoryRoleStore()
                : (IRoleStore)new CouchDbRoleStore(DbService(), Logger, EventContextResolverService);

            var userStore = useInMemoryDB
                ? new InMemoryUserStore()
                : (IUserStore)new CouchDbUserStore(DbService(), Logger, EventContextResolverService);

            var clientStore = useInMemoryDB
                ? new InMemoryClientStore()
                : (IClientStore)new CouchDbClientStore(DbService(), Logger, EventContextResolverService);

            _groupService = new GroupService(groupStore, roleStore, userStore);
            _clientService = new ClientService(clientStore);
            var roleService = new RoleService(roleStore, new InMemoryPermissionStore(), _clientService);

            Browser = new Browser(with =>
            {
                with.Module(new GroupsModule(
                    _groupService,
                    new GroupValidator(_groupService),
                    Logger,
                    DefaultPropertySettings));

                with.Module(new RolesModule(
                    roleService,
                    _clientService,
                    new RoleValidator(roleService),
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

            // create roles
            Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "rolesprincipal");
                with.FormValue("Name", UserAtlasRoleName);
            }).Wait();

            // create groups
        }
    }
}