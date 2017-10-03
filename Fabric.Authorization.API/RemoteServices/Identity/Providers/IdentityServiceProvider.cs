using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.RemoteServices.Identity.Models;
using Fabric.Platform.Http;
using IdentityModel.Client;
using Newtonsoft.Json;

namespace Fabric.Authorization.API.RemoteServices.Identity.Providers
{
    public class IdentityServiceProvider : IIdentityServiceProvider
    {
        private readonly IAppConfiguration _appConfiguration;
        private readonly IHttpClientFactory _httpClientFactory;

        public IdentityServiceProvider(IHttpClientFactory httpClientFactory, IAppConfiguration appConfiguration)
        {
            _httpClientFactory = httpClientFactory;
            _appConfiguration = appConfiguration;
        }

        public async Task<IEnumerable<UserSearchResponse>> Search(string clientId, IEnumerable<string> userIds)
        {
            var userIdList = userIds.ToList();
            if (userIdList.Count == 0)
            {
                return new List<UserSearchResponse>();
            }

            // TODO: clean this up / move to config
            var settings = _appConfiguration.IdentityServerConfidentialClientSettings;
            var httpClient = await Create(
                $"{settings.Authority}/connect/token",
                "fabric-authorization-client",
                settings.ClientSecret,
                new Uri(settings.Authority),
                IdentityScopes.ReadScope);

            var request = new UserSearchRequest
            {
                ClientId = clientId,
                UserIds = userIdList
            };

            var response = await httpClient.PostAsync("/api/users",
                new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json"));

            return response.StatusCode != HttpStatusCode.OK
                ? new List<UserSearchResponse>()
                : JsonConvert.DeserializeObject<IEnumerable<UserSearchResponse>>(await response.Content
                    .ReadAsStringAsync());
        }

        private async Task<HttpClient> Create(string tokenUrl, string clientId, string clientSecret, Uri uri, string requestScope)
        {
            var tokenClient = new TokenClient(tokenUrl, clientId, clientSecret);
            var response = await tokenClient.RequestClientCredentialsAsync(requestScope).ConfigureAwait(false);
            return CreateWithAccessToken(uri, response.AccessToken);
        }

        private HttpClient CreateWithAccessToken(Uri uri, string accessToken)
        {
            var client = new HttpClient { BaseAddress = uri };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            client.DefaultRequestHeaders.Add("correlation-token", string.Empty);
            return client;
        }
    }
}