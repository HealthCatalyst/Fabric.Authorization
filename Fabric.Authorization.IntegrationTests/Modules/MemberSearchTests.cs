using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Catalyst.Fabric.Authorization.Models;
using Catalyst.Fabric.Authorization.Models.Search;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.RemoteServices.Identity.Models;
using Fabric.Authorization.API.RemoteServices.Identity.Providers;
using Fabric.Authorization.Domain.Services;
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
            Fixture.InitializeAtlasBrowser(new Mock<IIdentityServiceProvider>().Object);

            var result = await Fixture.Browser.Get(
                MemberSearchRoute,
                with =>
                    {
                        with.HttpRequest();
                        with.Header("Accept", "application/json");
                        with.Query("client_id", "blah");
                        with.Query("sort_key", "name");
                        with.Query("sort_direction", "desc");
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
                        with.Query("sort_direction", "desc");
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
                        with.Query("sort_direction", "desc");
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
        public async Task MemberSearch_MissingRequiredRequestParameters_BadRequestExceptionAsync()
        {
            Fixture.InitializeAtlasBrowser(new Mock<IIdentityServiceProvider>().Object);

            var result = await Fixture.Browser.Get(
                MemberSearchRoute,
                with =>
                {
                    with.HttpRequest();
                    with.Header("Accept", "application/json");
                });

            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task MemberSearch_BadRequestParameters_BadRequestExceptionAsync()
        {
            Fixture.InitializeAtlasBrowser(new Mock<IIdentityServiceProvider>().Object);

            var result = await Fixture.Browser.Get(
                MemberSearchRoute,
                with =>
                {
                    with.HttpRequest();
                    with.Header("Accept", "application/json");
                    with.Query("client_id", Fixture.AtlasClientId);
                    with.Query("grain", "app");
                });

            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task MemberSearch_ValidClientIdRequest_SuccessAsync()
        {
            var lastLoginDate = new DateTime(2017, 9, 15).ToUniversalTime();

            var mockIdentityServiceProvider = CreateIdentityServiceProviderMock(Fixture.AtlasClientId, lastLoginDate);
            await Fixture.InitializeSuccessDataAsync(mockIdentityServiceProvider);

            var response = await Fixture.Browser.Get(
                MemberSearchRoute,
                with =>
                {
                    with.HttpRequest();
                    with.Header("Accept", "application/json");
                    with.Query("client_id", Fixture.AtlasClientId);
                    with.Query("sort_key", "name");
                    with.Query("sort_direction", "desc");
                    with.Query("filter", "brian");
                    with.Query("page_number", "1");
                    with.Query("page_size", "2");
                });

            // ensure user has expected roles
            var userResponse = await Fixture.Browser.Get($"/user/Windows/{Fixture.AtlasUserNoGroupName}",
                with =>
                {
                    with.HttpRequest();
                    with.Header("Accept", "application/json");
                });

            var userApiModel = userResponse.Body.DeserializeJson<UserApiModel>();
            Assert.True(userApiModel.Roles.Count == 2, $"UserApiModel Role count = {userApiModel.Roles.Count}, UserApiModel.Roles = ${string.Join(",", userApiModel.Roles)}");

            var results = response.Body.DeserializeJson<MemberSearchResponseApiModel>();
            AssertValidRequest(results, lastLoginDate);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task MemberSearch_ValidGrainSecurableItemRequest_SuccessAsync()
        {
            var lastLoginDate = new DateTime(2017, 9, 15).ToUniversalTime();

            var mockIdentityServiceProvider = CreateIdentityServiceProviderMock(null, lastLoginDate);
            await Fixture.InitializeSuccessDataAsync(mockIdentityServiceProvider);

            // test grain/securable_item QS parameters
            var response = await Fixture.Browser.Get(
                MemberSearchRoute,
                with =>
                {
                    with.HttpRequest();
                    with.Header("Accept", "application/json");
                    with.Query("grain", "app");
                    with.Query("securable_item", Fixture.AtlasClientId);
                    with.Query("sort_key", "name");
                    with.Query("sort_direction", "desc");
                    with.Query("filter", "brian");
                    with.Query("page_number", "1");
                    with.Query("page_size", "2");
                });

            var results = response.Body.DeserializeJson<MemberSearchResponseApiModel>();
            AssertValidRequest(results, lastLoginDate);
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task MemberSearch_ValidRequest_GroupWithMultipleRoles_SuccessAsync()
        {
            var groupName = Fixture.UserAtlasGroupName;

            var lastLoginDate = new DateTime(2017, 9, 15).ToUniversalTime();

            var mockIdentityServiceProvider = CreateIdentityServiceProviderMock(null, lastLoginDate);
            await Fixture.InitializeSuccessDataAsync(mockIdentityServiceProvider);

            // test grain/securable_item QS parameters
            var response = await Fixture.Browser.Get(
                MemberSearchRoute,
                with =>
                {
                    with.HttpRequest();
                    with.Header("Accept", "application/json");
                    with.Query("grain", "app");
                    with.Query("securable_item", Fixture.AtlasClientId);
                    with.Query("filter", groupName);
                });

            var resultModel = response.Body.DeserializeJson<MemberSearchResponseApiModel>();
            var results = resultModel.Results.ToList();
            Assert.Single(results);

            //ensure the group has two roles 
            var result = results.First();
            Assert.Equal(groupName, result.DisplayName);
            Assert.Equal(MemberSearchResponseEntityType.CustomGroup.ToString(), result.EntityType);
            Assert.Equal(2, result.Roles.Count());

        }

        [Fact]
        public async Task MemberSearch_ValidRequest_NoPageSize_TotalCountCorrect_SuccessAsync()
        {
            var lastLoginDate = new DateTime(2017, 9, 15).ToUniversalTime();

            var mockIdentityServiceProvider = CreateIdentityServiceProviderMock(null, lastLoginDate);
            await Fixture.InitializeSuccessDataAsync(mockIdentityServiceProvider);

            var response = await Fixture.Browser.Get(
                MemberSearchRoute,
                with =>
                {
                    with.HttpRequest();
                    with.Header("Accept", "application/json");
                    with.Query("grain", "app");
                    with.Query("securable_item", Fixture.AtlasClientId);
                });

            var resultModel = response.Body.DeserializeJson<MemberSearchResponseApiModel>();
            //no page size so total count should match result count
            Assert.NotEmpty(resultModel.Results);
            Assert.Equal(resultModel.Results.Count(), resultModel.TotalCount);
        }

        [Fact]
        public async Task MemberSearch_ValidRequest_PageSizeSet_TotalCountCorrect_SuccessAsync()
        {
            var lastLoginDate = new DateTime(2017, 9, 15).ToUniversalTime();

            var mockIdentityServiceProvider = CreateIdentityServiceProviderMock(null, lastLoginDate);
            await Fixture.InitializeSuccessDataAsync(mockIdentityServiceProvider);

            var response = await Fixture.Browser.Get(
                MemberSearchRoute,
                with =>
                {
                    with.HttpRequest();
                    with.Header("Accept", "application/json");
                    with.Query("grain", "app");
                    with.Query("securable_item", Fixture.AtlasClientId);
                    with.Query("page_number", "1");
                    with.Query("page_size", "1");
                });

            var resultModel = response.Body.DeserializeJson<MemberSearchResponseApiModel>();
            //page size set so total count should be greater than results count
            Assert.NotEmpty(resultModel.Results);
            Assert.Single(resultModel.Results);
            Assert.True(resultModel.TotalCount > resultModel.Results.Count());
        }
        
        private IIdentityServiceProvider CreateIdentityServiceProviderMock(string clientId, DateTime? lastLoginDate)
        {
            var mockIdentityServiceProvider = new Mock<IIdentityServiceProvider>();
            mockIdentityServiceProvider
                .Setup(m => m.Search(clientId, It.IsAny<IEnumerable<string>>())).ReturnsAsync(
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

            return mockIdentityServiceProvider.Object;
        }

        private void AssertValidRequest(MemberSearchResponseApiModel result, DateTime? lastLoginDate)
        {
            var results = result.Results.ToList();            
            Assert.Equal(2, results.Count);

            var result1 = results[0];
            Assert.Equal(Fixture.AtlasUserNoGroupName, result1.SubjectId);
            Assert.Equal("Shawn", result1.FirstName);
            Assert.Equal("James", result1.MiddleName);
            Assert.Equal("Brian", result1.LastName);
            Assert.Equal("Shawn Brian", result1.DisplayName);
            Assert.NotNull(result1.LastLoginDateTimeUtc);
            Assert.Equal(lastLoginDate, result1.LastLoginDateTimeUtc.Value.ToUniversalTime());
            Assert.True(2 == result1.Roles.Count(), $"Role count = {result1.Roles.Count()}, roles = ${string.Join(",", result1.Roles)}");
            Assert.Contains(Fixture.ContributorAtlasRoleName, result1.Roles.Select(r => r.Name));

            var result2 = results[1];
            Assert.Equal(Fixture.AtlasUserName, result2.SubjectId);
            Assert.Equal("Robert", result2.FirstName);
            Assert.Equal("Brian", result2.MiddleName);
            Assert.Equal("Smith", result2.LastName);
            Assert.Equal("Robert Smith", result2.DisplayName);
            Assert.NotNull(result2.LastLoginDateTimeUtc);
            Assert.Equal(lastLoginDate, result2.LastLoginDateTimeUtc.Value.ToUniversalTime());
            Assert.Single(result2.Roles);
            Assert.Contains(Fixture.ContributorAtlasRoleName, result2.Roles.Select(r => r.Name));
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task MemberSearch_NoParams_BadRequestExceptionAsync()
        {
            Fixture.InitializeAtlasBrowser(new Mock<IIdentityServiceProvider>().Object);
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
        public string PatientSafetyClientId { get; private set; }
        public string AdminPatientSafetyRoleName { get; private set; }

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
            AtlasUserNoGroupName = $"atlas_user_no_group-{DateTime.Now.Ticks}";
            AtlasUserName = $"atlas_user-{DateTime.Now.Ticks}";

            PatientSafetyClientId = $"patientsafety-{DateTime.Now.Ticks}";
            AdminPatientSafetyRoleName = $"adminPatientSafety-{DateTime.Now.Ticks}";

            IdentityProvider = "Windows";
        }

        public void InitializeAtlasBrowser(IIdentityServiceProvider identityServiceProvider)
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

        private Browser InitializePatientSafetyBrowser(IIdentityServiceProvider identityServiceProvider)
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(Claims.Scope, Scopes.ManageClientsScope),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.ClientId, PatientSafetyClientId),
                new Claim(Claims.IdentityProvider, "idP1")
            }, "rolesprincipal"));
            return GetBrowser(principal, _storageProvider, identityServiceProvider);
        }

        public async Task InitializeSuccessDataAsync(IIdentityServiceProvider identityServiceProvider)
        {
            InitializeAtlasBrowser(identityServiceProvider);

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

            // create the Patient Safety client
            var browser = InitializePatientSafetyBrowser(identityServiceProvider);
            response = await browser.Post("/clients", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Id = PatientSafetyClientId,
                    Name = PatientSafetyClientId
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

            var contributorAtlasRoleResponse = await Browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = AtlasClientId,
                    Name = ContributorAtlasRoleName
                });
            });

            Assert.Equal(HttpStatusCode.Created, contributorAtlasRoleResponse.StatusCode);

            var adminPatientSafetyRole = await browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "app",
                    SecurableItem = PatientSafetyClientId,
                    Name = AdminPatientSafetyRoleName
                });
            });

            Assert.Equal(HttpStatusCode.Created, adminPatientSafetyRole.StatusCode);

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
                    GroupSource = GroupConstants.DirectorySource,
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

            var contributorAtlasRole = contributorAtlasRoleResponse.Body.DeserializeJson<RoleApiModel>();

            //add second role to the group
            response = await Browser.Post($"/groups/{UserAtlasGroupName}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new
                    {
                        contributorAtlasRole.Grain,
                        contributorAtlasRole.SecurableItem,
                        contributorAtlasRole.Name,
                        contributorAtlasRole.Id
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
                        IdentityProvider
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
                    contributorAtlasRoleResponse.Body.DeserializeJson<RoleApiModel>()
                });
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // add a second role to the user (role is tied to a different client)
            response = await browser.Post($"/user/{IdentityProvider}/{AtlasUserName}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    adminPatientSafetyRole.Body.DeserializeJson<RoleApiModel>()
                });
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            //add user with role but no group 
            response = await Browser.Post("/user/", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    SubjectId = AtlasUserNoGroupName,
                    IdentityProvider
                });
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            //add roles to user            
            response = await Browser.Post($"/user/{IdentityProvider}/{AtlasUserNoGroupName}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    contributorAtlasRoleResponse.Body.DeserializeJson<RoleApiModel>(),
                    adminAtlasRole,
                    userAtlasRole
                });
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            //delete the userAtlasRole from the user
            response = await Browser.Delete($"/user/{IdentityProvider}/{AtlasUserNoGroupName}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {                    
                    userAtlasRole
                });
            });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        }

        public async Task InitializeClientWithoutRolesAsync(IIdentityServiceProvider identityServiceProvider)
        {
            InitializeAtlasBrowser(identityServiceProvider);

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
            InitializeAtlasBrowser(identityServiceProvider);

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