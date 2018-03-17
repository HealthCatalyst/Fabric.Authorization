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
using Fabric.Authorization.Persistence.SqlServer.Configuration;
using Moq;
using Nancy;
using Nancy.Testing;
using Xunit;

namespace Fabric.Authorization.IntegrationTests.Modules
{
    [Collection("InMemoryTests")]
    public class MemberSearchTests : IClassFixture<MemberSearchFixture>
    {
        protected readonly MemberSearchFixture Fixture;
        private static readonly string MemberSearchRoute = "/members";

        public MemberSearchTests(MemberSearchFixture fixture, ConnectionStrings connectionStrings = null)
        {
            if (connectionStrings != null)
            {
                fixture.ConnectionStrings = connectionStrings;
            }
            Fixture = fixture;
            Fixture.Initialize(StorageProviders.InMemory);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task MemberSearch_ClientIdDoesNotExist_NotFoundExceptionAsync()
        {
            Fixture.InitializeBrowser(new Mock<IIdentityServiceProvider>().Object);

            var result = await Fixture.Browser.Get(
                MemberSearchRoute,
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
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task MemberSearch_ClientWithoutRoles_EmptyResponseAsync()
        {
            var mockIdentityServiceProvider = new Mock<IIdentityServiceProvider>();
            await Fixture.InitializeClientWithoutRolesAsync(mockIdentityServiceProvider.Object);

            var response = await Fixture.Browser.Get(
                MemberSearchRoute,
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
            var results = response.Body.DeserializeJson<List<MemberSearchResponse>>();
            Assert.Empty(results);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task MemberSearch_ClientWithRolesAndNoGroups_EmptyResponseAsync()
        {
            var mockIdentityServiceProvider = new Mock<IIdentityServiceProvider>();
            await Fixture.InitializeClientWithRolesAndNoGroupsAsync(mockIdentityServiceProvider.Object);

            var response = await Fixture.Browser.Get(
                MemberSearchRoute,
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
            var results = response.Body.DeserializeJson<List<MemberSearchResponse>>();
            Assert.Empty(results);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task MemberSearch_ValidRequest_SuccessAsync()
        {
            var lastLoginDate = new DateTime(2017, 9, 15).ToUniversalTime();

            var mockIdentityServiceProvider = new Mock<IIdentityServiceProvider>();
            mockIdentityServiceProvider
                .Setup(m => m.Search(Fixture.AtlasClientId, new List<string>
                {
                    $"{Fixture.AtlasUserName}:{Fixture.IdentityProvider}",
                    $"{Fixture.AtlasUserNoGroupName}:{Fixture.IdentityProvider}"
                })).ReturnsAsync(
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
                                        Fixture.AtlasUserName,
                                    FirstName
                                        = "Robert",
                                    MiddleName
                                        = "Brian",
                                    LastName
                                        = "Smith",
                                    LastLoginDate
                                        = lastLoginDate
                                },
                            new
                                UserSearchResponse
                                {
                                    SubjectId
                                        =
                                        Fixture.AtlasUserNoGroupName,
                                    FirstName
                                        = "Shawn",
                                    MiddleName
                                        = "James",
                                    LastName
                                        = "Brian",
                                    LastLoginDate
                                        = lastLoginDate
                                }

                        }
                    });

            await Fixture.InitializeSuccessDataAsync(mockIdentityServiceProvider.Object);

            var response = await Fixture.Browser.Get(
                MemberSearchRoute,
                with =>
                    {
                        with.HttpRequest();
                        with.Header("Accept", "application/json");
                        with.Query("client_id", Fixture.AtlasClientId);
                        with.Query("sort_key", "name");
                        with.Query("sort_dir", "desc");
                        with.Query("filter", "brian");
                        with.Query("page_number", "1");
                        with.Query("page_size", "2");
                    });

            var results = response.Body.DeserializeJson<List<MemberSearchResponse>>();

            Assert.Equal(2, results.Count);

            var result1 = results[0];
            Assert.Equal(Fixture.AtlasUserName, result1.SubjectId);
            Assert.Equal("Robert", result1.FirstName);
            Assert.Equal("Brian", result1.MiddleName);
            Assert.Equal("Smith", result1.LastName);
            Assert.NotNull(result1.LastLoginDateTimeUtc);
            Assert.Equal(lastLoginDate, result1.LastLoginDateTimeUtc.Value.ToUniversalTime());
            Assert.Single(result1.Roles);
            Assert.Contains(Fixture.ContributorAtlasRoleName, result1.Roles.Select(r => r.Name));

            var result2 = results[1];
            Assert.Equal(Fixture.AtlasUserNoGroupName, result2.SubjectId);
            Assert.Equal("Shawn", result2.FirstName);
            Assert.Equal("James", result2.MiddleName);
            Assert.Equal("Brian", result2.LastName);
            Assert.NotNull(result2.LastLoginDateTimeUtc);
            Assert.Equal(lastLoginDate, result2.LastLoginDateTimeUtc.Value.ToUniversalTime());
            Assert.Single(result2.Roles);
            Assert.Contains(Fixture.ContributorAtlasRoleName, result2.Roles.Select(r => r.Name));
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task MemberSearch_NoParams_BadRequestExceptionAsync()
        {
            Fixture.InitializeBrowser(new Mock<IIdentityServiceProvider>().Object);
            var result = await Fixture.Browser.Get(
                MemberSearchRoute,
                with =>
                    {
                        with.HttpRequest();
                        with.Header("Accept", "application/json");
                    });

            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }        
    }

    public class MemberSearchFixture : IntegrationTestsFixture
    {
        public string AtlasClientId { get; private set; }
        public string AdminAtlasGroupName { get; private set; }
        public string UserAtlasGroupName { get; private set; }
        public string AdminAtlasRoleName { get; private set; }
        public string UserAtlasRoleName { get; private set; }
        public string ContributorAtlasRoleName { get; private set; }
        public string IdentityProvider { get; private set; }
        public string AtlasUserName { get; private set; }
        public string AtlasUserNoGroupName { get; private set; }
        
        private string _storageProvider;

        public void Initialize(string storageProvider)
        {
            _storageProvider = storageProvider;
            AtlasClientId = $"atlas-{DateTime.Now.Ticks}";
            AdminAtlasGroupName = $"adminAtlasGroup-{DateTime.Now.Ticks}";
            UserAtlasGroupName = $"userAtlasGroup-{DateTime.Now.Ticks}";
            AdminAtlasRoleName = $"adminAtlasRole-{DateTime.Now.Ticks}";
            UserAtlasRoleName = $"userAtlasRole-{DateTime.Now.Ticks}";
            ContributorAtlasRoleName = $"contributorAtlas-Role-{DateTime.Now.Ticks}";
            AtlasUserNoGroupName = "atlas_user_no_group";
            AtlasUserName = "atlas_user";
            IdentityProvider = "Windows";
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

            var contributorAtlasRole = await Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = AtlasClientId,
                    Name = ContributorAtlasRoleName
                });
            });

            Assert.Equal(HttpStatusCode.Created, contributorAtlasRole.StatusCode);

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

            var adminAtlasRole = adminAtlasRoleResponse.Body.DeserializeJson<RoleApiModel>();

            // add role to group
            response = await Browser.Post($"/groups/{AdminAtlasGroupName}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new
                    {
                        adminAtlasRole.Grain,
                        adminAtlasRole.SecurableItem,
                        adminAtlasRole.Name,
                        adminAtlasRole.Id
                    }
                });
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var userAtlasRole = userAtlasRoleResponse.Body.DeserializeJson<RoleApiModel>();

            response = await Browser.Post($"/groups/{UserAtlasGroupName}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new
                    {
                        userAtlasRole.Grain,
                        userAtlasRole.SecurableItem,
                        userAtlasRole.Name,
                        userAtlasRole.Id
                    }
                });
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // add user to custom group
            response = await Browser.Post($"/groups/{UserAtlasGroupName}/users", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new
                    {
                        SubjectId = AtlasUserName,
                        IdentityProvider = IdentityProvider
                    }
                });
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            //add role to user
            response = await Browser.Post($"/user/{IdentityProvider}/{AtlasUserName}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new []
                {
                    contributorAtlasRole.Body.DeserializeJson<RoleApiModel>()
                });
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            //add user with role but no group 
            response = await Browser.Post($"/user/", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    SubjectId = AtlasUserNoGroupName,
                    IdentityProvider = IdentityProvider
                });
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            //add role to user            
            response = await Browser.Post($"/user/{IdentityProvider}/{AtlasUserNoGroupName}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    contributorAtlasRole.Body.DeserializeJson<RoleApiModel>()
                });
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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