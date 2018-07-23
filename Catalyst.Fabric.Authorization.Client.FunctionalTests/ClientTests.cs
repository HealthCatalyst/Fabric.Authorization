using System;
using System.Net;
using Catalyst.Fabric.Authorization.Models;
using Xunit;

namespace Catalyst.Fabric.Authorization.Client.FunctionalTests
{
    [Collection(FunctionalTestConstants.FunctionTestTitle)]
    public class ClientTests : BaseTest
    {
        public ClientTests(FunctionalTestFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public async void AddAndGetClient_ValidRequest_Success()
        {
            var accessToken = await fixture.GetAccessTokenForInstaller();

            var clientId = Guid.NewGuid().ToString();

            var client = await _authorizationClient.AddClient(accessToken, new ClientApiModel
            {
                Id = clientId,
                Name = clientId
            });

            Assert.NotNull(client);

            client = await _authorizationClient.GetClient(accessToken, clientId);
            Assert.NotNull(client);
            Assert.Equal(clientId, client.Id);
            Assert.Equal(clientId, client.Name);
        }

        [Fact]
        public async void AddClient_InvalidRequest_Exception()
        {
            try
            {
                var accessToken = await fixture.GetAccessTokenForInstaller();
                await _authorizationClient.AddClient(accessToken, new ClientApiModel());
            }
            catch (AuthorizationException e)
            {
                Assert.Equal(e.Details.Code, HttpStatusCode.BadRequest.ToString());
            }
        }
    }
}