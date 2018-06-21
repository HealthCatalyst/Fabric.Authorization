using System.Net.Http;
using System.Threading.Tasks;
using Fabric.Authorization.Client.Routes;
using Fabric.Authorization.Models;
using Newtonsoft.Json;

namespace Fabric.Authorization.Client
{
    public class AuthorizationClient
    {
        private readonly HttpClient client;

        public AuthorizationClient(HttpClient client)
        {
            this.client = client;
        }

        public async Task<UserApiModel> GetPermissionsForCurrentUser()
        {
            var route = new UserRouteBuilder().UserPermissionsRoute;
            var message = new HttpRequestMessage(HttpMethod.Get, route);
            return await SendAndParseJson<UserApiModel>(message).ConfigureAwait(false);
        }

        private async Task<T> SendAndParseJson<T>(HttpRequestMessage message)
        {
            var response = await client.SendAsync(message).ConfigureAwait(false);
            var stringResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(stringResponse);
            }

            var error = JsonConvert.DeserializeObject<Error>(stringResponse);
            throw new AuthorizationException(error);
        }
    }
}