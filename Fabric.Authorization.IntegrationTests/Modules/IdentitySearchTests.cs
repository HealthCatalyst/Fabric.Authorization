using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.API.Models.Search;
using Fabric.Authorization.API.RemoteServices.Identity.Models;
using Fabric.Authorization.API.RemoteServices.Identity.Providers;
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
            Fixture.Initialize(StorageProviders.InMemory);
        }

        [Fact]
        public async Task IdentitySearch_ClientIdDoesNotExist_NotFoundExceptionAsync()
        {
            Fixture.InitializeBrowser(new Mock<IIdentityServiceProvider>().Object);

            var result = await Fixture.Browser.Get(
                "/identities",
                with =>
                    {
                        with.HttpRequest();
                        with.Header("Accept", "application/json");
                        with.Query("client_id", "blah");
                        with.Query("sort_key", "name");
                        with.Query("sort_dir", "desc");
                        with.Query("filter", "brian");
                        with.Query("page_number", "1");
                        with.Query("page_size", "1");
                    });

            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        }

        [Fact]
        public async Task IdentitySearch_ClientWithoutRoles_EmptyResponseAsync()
        {
            var mockIdentityServiceProvider = new Mock<IIdentityServiceProvider>();
            await Fixture.InitializeClientWithoutRolesAsync(mockIdentityServiceProvider.Object);

            var response = await Fixture.Browser.Get(
                "/identities",
                with =>
                    {
                        with.HttpRequest();
                        with.Header("Accept", "application/json");
                        with.Query("client_id", Fixture.AtlasClientId);
                        with.Query("sort_key", "name");
                        with.Query("sort_dir", "desc");
                        with.Query("filter", "brian");
                        with.Query("page_number", "1");
                        with.Query("page_size", "1");
                    });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var results = response.Body.DeserializeJson<List<IdentitySearchResponse>>();
            Assert.Empty(results);
        }

        [Fact]
        public async Task IdentitySearch_ClientWithRolesAndNoGroups_EmptyResponseAsync()
        {
            var mockIdentityServiceProvider = new Mock<IIdentityServiceProvider>();
            await Fixture.InitializeClientWithRolesAndNoGroupsAsync(mockIdentityServiceProvider.Object);

            var response = await Fixture.Browser.Get(
                "/identities",
                with =>
                    {
                        with.HttpRequest();
                        with.Header("Accept", "application/json");
                        with.Query("client_id", Fixture.AtlasClientId);
                        with.Query("sort_key", "name");
                        with.Query("sort_dir", "desc");
                        with.Query("filter", "brian");
                        with.Query("page_number", "1");
                        with.Query("page_size", "1");
                    });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var results = response.Body.DeserializeJson<List<IdentitySearchResponse>>();
            Assert.Empty(results);
        }

        [Fact]
        public async Task IdentitySearch_ValidRequest_SuccessAsync()
        {
            var lastLoginDate = new DateTime(2017, 9, 15).ToUniversalTime();

            var mockIdentityServiceProvider = new Mock<IIdentityServiceProvider>();
            mockIdentityServiceProvider
                .Setup(m => m.Search(Fixture.AtlasClientId, new List<string> { "atlas_user:Windows" })).ReturnsAsync(
                    () => new FabricIdentityUserResponse
                              {
                                  HttpStatusCode = System.Net.HttpStatusCode.OK,
                                  Results = new List<UserSearchResponse>
                                                {
                                                    new
                                                        UserSearchResponse
                                                            {
                                                                SubjectId
                                                                    =
                                                                    "atlas_user",
                                                                FirstName
                                                                    = "Robert",
                                                                MiddleName
                                                                    = "Brian",
                                                                LastName
                                                                    = "Smith",
                                                                LastLoginDate
                                                                    = lastLoginDate
                                                            }
                                                }
                              });

            await Fixture.InitializeSuccessDataAsync(mockIdentityServiceProvider.Object);

            var response = await Fixture.Browser.Get(
                "/identities",
                with =>
                    {
                        with.HttpRequest();
                        with.Header("Accept", "application/json");
                        with.Query("client_id", Fixture.AtlasClientId);
                        with.Query("sort_key", "name");
                        with.Query("sort_dir", "desc");
                        with.Query("filter", "brian");
                        with.Query("page_number", "1");
                        with.Query("page_size", "1");
                    });

            var results = response.Body.DeserializeJson<List<IdentitySearchResponse>>();

            Assert.Single(results);

            var result1 = results[0];
            Assert.Equal("atlas_user", result1.SubjectId);
            Assert.Equal("Robert", result1.FirstName);
            Assert.Equal("Brian", result1.MiddleName);
            Assert.Equal("Smith", result1.LastName);
            Assert.NotNull(result1.LastLoginDateTimeUtc);
            Assert.Equal(lastLoginDate, result1.LastLoginDateTimeUtc.Value.ToUniversalTime());
            Assert.Equal(Fixture.UserAtlasRoleName, result1.Roles.FirstOrDefault());
        }

        [Fact]
        public async Task IdentitySearch_NoParams_BadRequestExceptionAsync()
        {
            Fixture.InitializeBrowser(new Mock<IIdentityServiceProvider>().Object);
            var result = await Fixture.Browser.Get(
                "/identities",
                with =>
                    {
                        with.HttpRequest();
                        with.Header("Accept", "application/json");
                    });

            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }
    }

    public class IdentitySearchFixture : IntegrationTestsFixture
    {
        public string AtlasClientId { get; private set; }
        public string AdminAtlasGroupName { get; private set; }
        public string UserAtlasGroupName { get; private set; }
        public string AdminAtlasRoleName { get; private set; }
        public string UserAtlasRoleName { get; private set; }
        
        private string _storageProvider;

        public void Initialize(string storageProvider)
        {
            _storageProvider = storageProvider;
            AtlasClientId = $"atlas-{DateTime.Now.Ticks}";
            AdminAtlasGroupName = $"adminAtlasGroup-{DateTime.Now.Ticks}";
            UserAtlasGroupName = $"userAtlasGroup-{DateTime.Now.Ticks}";
            AdminAtlasRoleName = $"adminAtlasRole-{DateTime.Now.Ticks}";
            UserAtlasRoleName = $"userAtlasRole-{DateTime.Now.Ticks}";
        }

        public void InitializeBrowser(IIdentityServiceProvider identityServiceProvider)
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(Claims.Scope, Scopes.ManageClientsScope),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.ClientId, AtlasClientId),
                new Claim(Claims.IdentityProvider, "idP1")
            }, "rolesprincipal"));
            Browser = GetBrowser(principal, _storageProvider, identityServiceProvider);
        }

        public async Task InitializeSuccessDataAsync(IIdentityServiceProvider identityServiceProvider)
        {
            InitializeBrowser(identityServiceProvider);

            // create the Atlas client
            var response = await Browser.Post("/clients", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Id = AtlasClientId,
                    Name = AtlasClientId
                });
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // create roles
            var userAtlasRoleResponse = await Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = AtlasClientId,
                    Name = UserAtlasRoleName
                });
            });

            Assert.Equal(HttpStatusCode.Created, userAtlasRoleResponse.StatusCode);

            var adminAtlasRoleResponse = await Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = AtlasClientId,
                    Name = AdminAtlasRoleName
                });
            });

            Assert.Equal(HttpStatusCode.Created, adminAtlasRoleResponse.StatusCode);

            // create groups
            response = await Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = UserAtlasGroupName,
                    GroupSource = "Custom",
                });
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            response = await Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = AdminAtlasGroupName,
                    GroupSource = "Windows",
                });
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // add role to group
            response = await Browser.Post($"/groups/{AdminAtlasGroupName}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Id = adminAtlasRoleResponse.Body.DeserializeJson<RoleApiModel>().Id.ToString()
                });
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            response = await Browser.Post($"/groups/{UserAtlasGroupName}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Id = userAtlasRoleResponse.Body.DeserializeJson<RoleApiModel>().Id.ToString()
                });
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // add user to custom group
            response = await Browser.Post($"/groups/{UserAtlasGroupName}/users", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    SubjectId = "atlas_user",
                    IdentityProvider = "Windows"
                });
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        public async Task InitializeClientWithoutRolesAsync(IIdentityServiceProvider identityServiceProvider)
        {
            InitializeBrowser(identityServiceProvider);

            // create the Atlas client
            var response = await Browser.Post("/clients", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Id = AtlasClientId,
                    Name = AtlasClientId
                });
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        public async Task InitializeClientWithRolesAndNoGroupsAsync(IIdentityServiceProvider identityServiceProvider)
        {
            InitializeBrowser(identityServiceProvider);

            // create the Atlas client
            var response = await Browser.Post("/clients", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Id = AtlasClientId,
                    Name = AtlasClientId
                });
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // create roles
            response = await Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = AtlasClientId,
                    Name = UserAtlasRoleName
                });
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            response = await Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = AtlasClientId,
                    Name = AdminAtlasRoleName
                });
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
    }
}