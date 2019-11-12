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
using Fabric.Authorization.Domain.Models;
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

        private const string ServiceName = "Fabric.Identity";
        private const string UsersEndpoint = "users";
        private const string PrincipalsEndpoint = "principals/";

        public IdentityServiceProvider(
            HttpClient httpClient,
            IHttpRequestMessageFactory httpRequestMessageFactory,
            IAppConfiguration appConfiguration,
            ILogger logger)
        {
            _httpClient = httpClient;
            _httpRequestMessageFactory = httpRequestMessageFactory;
            _appConfiguration = appConfiguration;
            _logger = logger;
        }

        public async Task<FabricIdentityUserResponse> SearchUsersAsync(string clientId, IEnumerable<string> userIds)
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

            var httpRequestMessage = await CreateHttpRequestMessage(UsersEndpoint, HttpMethod.Post);

            var request = new UserSearchRequest
            {
                ClientId = clientId,
                UserIds = userIdList
            };

            _logger.Debug($"Invoking {ServiceName} endpoint {httpRequestMessage.RequestUri}");

            httpRequestMessage.Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(httpRequestMessage);
            var responseContent = response.Content == null ? string.Empty : await response.Content.ReadAsStringAsync();
            
            var results = new List<UserSearchResponse>();
            if (response.StatusCode != HttpStatusCode.OK)
            {
                LogNonOkResponse(httpRequestMessage, response, responseContent);
            }
            else
            {
                results = JsonConvert.DeserializeObject<List<UserSearchResponse>>(responseContent);
                _logger.Debug($"{ServiceName} {httpRequestMessage.RequestUri} results: {results}");
            }

            return new FabricIdentityUserResponse
            {
                HttpStatusCode = response.StatusCode,
                Results = results
            };
        }

        public async Task<FabricIdentityGroupResponse> SearchGroupAsync(GroupSearchRequest request)
        {
            var route = $"{PrincipalsEndpoint}{request.IdentityProvider}/groups/{request.DisplayName}";

            if (!string.IsNullOrWhiteSpace(request.TenantId))
            {
                route = $"{route}?tenantId={request.TenantId}";
            }

            var httpRequestMessage = await CreateHttpRequestMessage(route, HttpMethod.Get);

            _logger.Debug($"Invoking {ServiceName} endpoint {httpRequestMessage.RequestUri}");

            httpRequestMessage.Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(httpRequestMessage);
            var responseContent = response.Content == null ? string.Empty : await response.Content.ReadAsStringAsync();
            
            var results = new GroupSearchResponse();
            if (response.StatusCode != HttpStatusCode.OK)
            {
                this.LogNonOkResponse(httpRequestMessage, response, responseContent);
            }
            else
            {
                results = JsonConvert.DeserializeObject<GroupSearchResponse>(responseContent);
                _logger.Debug($"{ServiceName} {httpRequestMessage.RequestUri} result: {results}");
            }

            return new FabricIdentityGroupResponse
            {
                HttpStatusCode = response.StatusCode,
                Results = results.Principals
            };
        }

        private async Task<HttpRequestMessage> CreateHttpRequestMessage(string route, HttpMethod httpMethod)
        {
            var settings = _appConfiguration.IdentityServerConfidentialClientSettings;
            var authority = settings.Authority.EnsureTrailingSlash();
            var tokenUriAddress = $"{authority}connect/token";
            var tokenClient = new TokenClient(tokenUriAddress, Domain.Identity.ClientName, settings.ClientSecret);
            var accessTokenResponse = await tokenClient
                                          .RequestClientCredentialsAsync(IdentityScopes.SearchUsersScope).ConfigureAwait(false);

            var baseUri = $"{authority}api/";
            var httpRequestMessage = _httpRequestMessageFactory.CreateWithAccessToken(httpMethod,
                new Uri($"{baseUri}{route}"),
                accessTokenResponse.AccessToken);

            httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return httpRequestMessage;
        }

        private void LogNonOkResponse(
            HttpRequestMessage httpRequestMessage,
            HttpResponseMessage response,
            string responseContent)
        {
            this._logger.Error($"Response status code from {ServiceName} {httpRequestMessage.RequestUri} => {response.StatusCode}");
            this._logger.Error($"Response content from {ServiceName} {httpRequestMessage.RequestUri} => {responseContent}");
        }
    }
}