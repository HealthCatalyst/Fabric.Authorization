using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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
            Fixture.Initialize(true);
        }

        [Fact]
        public void IdentitySearch_ClientIdDoesNotExist_NotFoundException()
        {
            Fixture.InitializeBrowser(new Mock<IIdentityServiceProvider>().Object);

            var result = Fixture.Browser.Get(
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
                    }).Result;

            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        }

        [Fact]
        public void IdentitySearch_ClientWithoutRoles_EmptyResponse()
        {
            var mockIdentityServiceProvider = new Mock<IIdentityServiceProvider>();
            Fixture.InitializeClientWithoutRoles(mockIdentityServiceProvider.Object);

            var response = Fixture.Browser.Get(
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
                    }).Result;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var results = response.Body.DeserializeJson<List<IdentitySearchResponse>>();
            Assert.Empty(results);
        }

        [Fact]
        public void IdentitySearch_ClientWithRolesAndNoGroups_EmptyResponse()
        {
            var mockIdentityServiceProvider = new Mock<IIdentityServiceProvider>();
            Fixture.InitializeClientWithRolesAndNoGroups(mockIdentityServiceProvider.Object);

            var response = Fixture.Browser.Get(
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
                    }).Result;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var results = response.Body.DeserializeJson<List<IdentitySearchResponse>>();
            Assert.Empty(results);
        }

        [Fact]
        public void IdentitySearch_ValidRequest_Success()
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

            Fixture.InitializeSuccessData(mockIdentityServiceProvider.Object);

            var response = Fixture.Browser.Get(
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
                    }).Result;

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
        public void IdentitySearch_NoParams_BadRequestException()
        {
            Fixture.InitializeBrowser(new Mock<IIdentityServiceProvider>().Object);
            var result = Fixture.Browser.Get(
                "/identities",
                with =>
                    {
                        with.HttpRequest();
                        with.Header("Accept", "application/json");
                    }).Result;

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
        
        private bool _useInMemoryDb;

        public void Initialize(bool useInMemoryDb)
        {
            _useInMemoryDb = useInMemoryDb;
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
            Browser = GetBrowser(principal, _useInMemoryDb, identityServiceProvider);
        }

        public void InitializeSuccessData(IIdentityServiceProvider identityServiceProvider)
        {
            InitializeBrowser(identityServiceProvider);

            // create the Atlas client
            var response = Browser.Post("/clients", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Id = AtlasClientId,
                    Name = AtlasClientId
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // create roles
            var userAtlasRoleResponse = Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = AtlasClientId,
                    Name = UserAtlasRoleName
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Created, userAtlasRoleResponse.StatusCode);

            var adminAtlasRoleResponse = Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = AtlasClientId,
                    Name = AdminAtlasRoleName
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Created, adminAtlasRoleResponse.StatusCode);

            // create groups
            response = Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = UserAtlasGroupName,
                    GroupSource = "Custom",
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            response = Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = AdminAtlasGroupName,
                    GroupSource = "Windows",
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // add role to group
            response = Browser.Post($"/groups/{AdminAtlasGroupName}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Id = adminAtlasRoleResponse.Body.DeserializeJson<RoleApiModel>().Id.ToString()
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            response = Browser.Post($"/groups/{UserAtlasGroupName}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Id = userAtlasRoleResponse.Body.DeserializeJson<RoleApiModel>().Id.ToString()
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // add user to custom group
            response = Browser.Post($"/groups/{UserAtlasGroupName}/users", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    SubjectId = "atlas_user",
                    IdentityProvider = "Windows"
                });
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
                with.JsonBody(new
                {
                    Id = AtlasClientId,
                    Name = AtlasClientId
                });
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
                with.JsonBody(new
                {
                    Id = AtlasClientId,
                    Name = AtlasClientId
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // create roles
            response = Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = AtlasClientId,
                    Name = UserAtlasRoleName
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            response = Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = AtlasClientId,
                    Name = AdminAtlasRoleName
                });
            }).Result;

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
    }
}