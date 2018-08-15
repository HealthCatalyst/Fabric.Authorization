using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Persistence.SqlServer.Configuration;
using Nancy;
using Nancy.Testing;
using Newtonsoft.Json;
using Xunit;
namespace Fabric.Authorization.IntegrationTests.Modules
{
    [Collection("InMemoryTests")]
    public class ChildGroupsTests : IClassFixture<IntegrationTestsFixture>
    {
        protected readonly Browser Browser;
        private readonly DefaultPropertySettings _defaultPropertySettings;
        private readonly IntegrationTestsFixture _fixture;
        private readonly string _storageProvider;

        protected ClaimsPrincipal Principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
        {
            new Claim(Claims.Scope, Scopes.ManageClientsScope),
            new Claim(Claims.Scope, Scopes.ReadScope),
            new Claim(Claims.Scope, Scopes.WriteScope),
            new Claim(Claims.ClientId, "rolesprincipal"),
            new Claim(Claims.IdentityProvider, "idP1")
        }, "rolesprincipal"));

        public ChildGroupsTests(IntegrationTestsFixture fixture, string storageProvider = StorageProviders.InMemory, ConnectionStrings connectionStrings = null)
        {
            if (connectionStrings != null)
            {
                fixture.ConnectionStrings = connectionStrings;
            }
            Browser = fixture.GetBrowser(Principal, storageProvider);
            _defaultPropertySettings = fixture.DefaultPropertySettings;
            fixture.CreateClient(Browser, "rolesprincipal");
            _fixture = fixture;
            _storageProvider = storageProvider;
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task AddChildGroup_ValidRequest_SuccessAsync()
        {
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task AddChildGroup_NonExistentChildGroup_BadRequestAsync()
        {
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task AddChildGroup_NonCustomParentGroup_BadRequestAsync()
        {
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task AddChildGroup_CustomChildGroup_BadRequestAsync()
        {
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task AddChildGroup_InsufficientPermissions_ForbiddenAsync()
        {
        }

        [Fact]
        [IntegrationTestsFixture.DisplayTestMethodName]
        public async Task GetUserPermissions_WithChildGroups_SuccessAsync()
        {
        }
    }
}
