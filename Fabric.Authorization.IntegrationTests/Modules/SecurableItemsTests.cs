using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.Persistence.SqlServer.Configuration;
using Nancy;
using Nancy.Testing;
using Xunit;

namespace Fabric.Authorization.IntegrationTests.Modules
{
    public class SecurableItemsTests : IClassFixture<IntegrationTestsFixture>
    {
        private readonly string _storageProvider;
        private readonly IntegrationTestsFixture _fixture;

        public SecurableItemsTests(IntegrationTestsFixture fixture, string storageProvider = StorageProviders.InMemory,
            ConnectionStrings connectionStrings = null)
        {
            if (connectionStrings != null)
            {
                fixture.ConnectionStrings = connectionStrings;
            }

            _fixture = fixture;
            _storageProvider = storageProvider;
        }

        /*[Fact]
        public async Task AddSecurableItemWithDosGrain_ValidScopeAndPermission_SuccessAsync()
        {
            var clientId = $"client-{Guid.NewGuid()}";

            var principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(Claims.Scope, Scopes.ManageClientsScope),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.ClientId, clientId)
            }, "pwd"));

            var browser = _fixture.GetBrowser(principal, _storageProvider);
            _fixture.CreateClient(browser, clientId);

            var get = await browser.Get($"/clients/{clientId}", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            var client = get.Body.DeserializeJson<ClientApiModel>();

            var securableItemApiModel = new SecurableItemApiModel
            {
                Name = $"dos-sec-item-{Guid.NewGuid()}",
                ClientOwner = clientId,
                Grain = Domain.Defaults.Authorization.DosGrain
            };

            var result = await browser.Post($"/securableitems/{client.TopLevelSecurableItem.Id}", with => with.JsonBody(securableItemApiModel));
            Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        }*/

        [Fact]
        public void AddSecurableItemWithDosGrain_InvalidScopeAndValidPermission_Failure()
        {

        }

        [Fact]
        public void AddSecurableItemWithDosGrain_InvalidPermissionAndValidScope_Failure()
        {

        }

        [Fact]
        public void AddChildSecurableItem_MismatchGrains_Failure()
        {

        }
    }
}
