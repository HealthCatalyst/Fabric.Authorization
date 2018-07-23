using System.Net.Http;

namespace Catalyst.Fabric.Authorization.Client.FunctionalTests
{
    public abstract class BaseTest
    {
        protected AuthorizationClient _authorizationClient;
        protected HttpClient _client;
        protected readonly FunctionalTestFixture fixture;

        protected BaseTest(FunctionalTestFixture fixture)
        {
            this.fixture = fixture;
            this._client = new HttpClient
            {
                BaseAddress = fixture.BaseUrl
            };

            this._authorizationClient = new AuthorizationClient(this._client);
        }
    }
}