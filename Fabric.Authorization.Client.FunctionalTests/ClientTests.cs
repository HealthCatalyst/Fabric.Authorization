using System;
using Fabric.Authorization.Models;
using Xunit;

namespace Fabric.Authorization.Client.FunctionalTests
{
    [Collection(FunctionalTestConstants.FunctionTestTitle)]
    public class ClientTests : BaseTest
    {
        public ClientTests(FunctionalTestFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public async void AddClient_ValidRequest_Success()
        {
            var accessToken = await fixture.GetAccessTokenForInstaller();

            var clientId = Guid.NewGuid().ToString();

            var client = await _authorizationClient.AddClient(accessToken, new ClientApiModel
            {
                Id = clientId,
                Name = clientId
            });

            Assert.NotNull(client);
        }

        [Fact]
        public void AddClient_InvalidRequest_Failure()
        {
            
        }

        [Fact]
        public void GetClient_ValidRequest_Success()
        {

        }
    }
}