using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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

            var tokenClient = new TokenClient($"{settings.Authority}/connect/token", "fabric-authorization-client", settings.ClientSecret);
            var accessTokenResponse = await tokenClient.RequestClientCredentialsAsync(IdentityScopes.ReadScope).ConfigureAwait(false);

            var httpClient = new HttpClientFactory(
                $"{settings.Authority}/connect/token",
                "fabric-authorization-client",
                settings.ClientSecret,
                null,
                null).CreateWithAccessToken(new Uri(settings.Authority), accessTokenResponse.AccessToken);

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
    }
}