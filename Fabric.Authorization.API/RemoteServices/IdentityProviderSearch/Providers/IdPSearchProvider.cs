using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.RemoteServices.IdentityProviderSearch.Models;
using Fabric.Authorization.Domain.Models;
using Fabric.Platform.Http;
using IdentityModel.Client;
using Newtonsoft.Json;
using Serilog;

namespace Fabric.Authorization.API.RemoteServices.IdentityProviderSearch.Providers
{
    public class IdPSearchProvider : IIdPSearchProvider
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpRequestMessageFactory _httpRequestMessageFactory;
        private readonly IAppConfiguration _appConfiguration;
        private readonly ILogger _logger;

        private const string ServiceName = "Fabric.IdentityProviderSearchService";

        public IdPSearchProvider(
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

        /// <summary>
        /// NOTE: IdPSearchService needs to be enhanced to expose this
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<FabricIdPSearchResponse> SearchAsync(IdPPrincipalSearchRequest request)
        {
            var route = $"/principals/search?searchText={request.SearchText}";

            if (!string.IsNullOrWhiteSpace(request.Type))
            {
                route = $"{route}&type={request.Type}";
            }

            var httpRequestMessage = await CreateHttpRequestMessage(route, HttpMethod.Get);

            _logger.Debug($"Invoking {ServiceName} endpoint {httpRequestMessage.RequestUri}");

            httpRequestMessage.Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8,
                "application/json");

            return await ProcessResponse<IdPPrincipalSearchResponse, FabricIdPSearchResponse>(httpRequestMessage);
        }

        public async Task<FabricIdPGroupResponse> GetGroupAsync(IdPGroupRequest request)
        {
            var route = $"{request.IdentityProvider}/groups/{request.DisplayName}";

            if (!string.IsNullOrWhiteSpace(request.Tenant))
            {
                route = $"{route}?tenant={request.Tenant}";
            }

            var httpRequestMessage = await CreateHttpRequestMessage(route, HttpMethod.Get);

            _logger.Debug($"Invoking {ServiceName} endpoint {httpRequestMessage.RequestUri}");

            httpRequestMessage.Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8,
                "application/json");

            return await ProcessResponse<IdPGroupResponse, FabricIdPGroupResponse>(httpRequestMessage);
        }

        private async Task<HttpRequestMessage> CreateHttpRequestMessage(string route, HttpMethod httpMethod)
        {
            var settings = _appConfiguration.IdentityServerConfidentialClientSettings;
            var authority = settings.Authority.EnsureTrailingSlash();
            var tokenUriAddress = $"{authority}connect/token";
            var tokenClient = new TokenClient(tokenUriAddress, "fabric-authorization-client", settings.ClientSecret);
            var accessTokenResponse = await tokenClient
                .RequestClientCredentialsAsync(IdentityProviderSearchScopes.SearchUsersScope).ConfigureAwait(false);

            var baseUri = _appConfiguration.IdentityProviderSearchSettings.Endpoint.EnsureTrailingSlash();
            var httpRequestMessage = _httpRequestMessageFactory.CreateWithAccessToken(httpMethod,
                new Uri($"{baseUri}/{route}"),
                accessTokenResponse.AccessToken);

            return httpRequestMessage;
        }

        private async Task<T1> ProcessResponse<T, T1>(HttpRequestMessage httpRequestMessage)
            where T1 : IFabricIdPResponseModel<T>, new()
            where T : new()
        {
            var response = await _httpClient.SendAsync(httpRequestMessage);
            var responseContent = response.Content == null ? string.Empty : await response.Content.ReadAsStringAsync();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                _logger.Error(
                    $"Response status code from {ServiceName} {httpRequestMessage.RequestUri} => {response.StatusCode}");
                _logger.Error(
                    $"Response content from {ServiceName} {httpRequestMessage.RequestUri} => {responseContent}");
            }
            else
            {
                var result = JsonConvert.DeserializeObject<T>(responseContent);
                _logger.Debug($"{ServiceName} {httpRequestMessage.RequestUri} result: {result}");

                return new T1
                {
                    HttpStatusCode = response.StatusCode,
                    Result = result
                };
            }

            return new T1();
        }
    }
}
