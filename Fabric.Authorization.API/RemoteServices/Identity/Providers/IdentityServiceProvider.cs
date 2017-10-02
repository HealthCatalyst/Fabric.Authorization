using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.RemoteServices.Identity.Models;
using Fabric.Platform.Http;
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
            // get all users in groups tied to clientRoles
            var httpClient =
                await _httpClientFactory.Create(
                    new Uri(_appConfiguration.IdentityServerConfidentialClientSettings.Authority),
                    IdentityScopes.ReadScope);

            var request = new UserSearchRequest
            {
                ClientId = clientId,
                UserIds = userIds
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