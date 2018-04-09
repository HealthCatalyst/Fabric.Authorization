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
using Serilog;

namespace Fabric.Authorization.API.RemoteServices.Identity.Providers
{
    public class IdentityServiceProvider : IIdentityServiceProvider
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpRequestMessageFactory _httpRequestMessageFactory;
        private readonly IAppConfiguration _appConfiguration;
        private readonly ILogger _logger;

        public IdentityServiceProvider(HttpClient httpClient, IHttpRequestMessageFactory httpRequestMessageFactory, IAppConfiguration appConfiguration, ILogger logger)
        {
            _httpClient = httpClient;
            _httpRequestMessageFactory = httpRequestMessageFactory;
            _appConfiguration = appConfiguration;
            _logger = logger;
        }

        public async Task<FabricIdentityUserResponse> Search(string clientId, IEnumerable<string> userIds)
        {
            var userIdList = userIds.ToList();
            if (userIdList.Count == 0)
            {
                return new FabricIdentityUserResponse
                {
                    Results = new List<UserSearchResponse>(),
                    HttpStatusCode = HttpStatusCode.OK
                };
            }

            // TODO: clean this up / move to config
            var settings = _appConfiguration.IdentityServerConfidentialClientSettings;

            var tokenUriAddress = $"{settings.Authority}connect/token";
            var tokenClient = new TokenClient(tokenUriAddress, "fabric-authorization-client", settings.ClientSecret);
            var accessTokenResponse = await tokenClient.RequestClientCredentialsAsync(IdentityScopes.SearchUsersScope).ConfigureAwait(false);

            var httpRequestMessage = _httpRequestMessageFactory.CreateWithAccessToken(HttpMethod.Post, new Uri($"{settings.Authority}api/users"),
                accessTokenResponse.AccessToken);

            var request = new UserSearchRequest
            {
                ClientId = clientId,
                UserIds = userIdList
            };

            _logger.Debug($"Invoking Fabric.Identity endpoint {httpRequestMessage.RequestUri}");

            httpRequestMessage.Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(httpRequestMessage);

            var results = new List<UserSearchResponse>();
            var responseContent = response.Content == null ? string.Empty : await response.Content.ReadAsStringAsync();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                _logger.Error($"Response status code from Fabric.Identity api/users => {response.StatusCode}");
                _logger.Error($"Response content from Fabric.Identity api/users => {responseContent}");
            }
            else
            {
                results = JsonConvert.DeserializeObject<List<UserSearchResponse>>(responseContent);

                _logger.Debug($"Fabric.Identity /users results: {results}");
            }

            return new FabricIdentityUserResponse
            {
                HttpStatusCode = response.StatusCode,
                Results = results
            };
        }
    }
}