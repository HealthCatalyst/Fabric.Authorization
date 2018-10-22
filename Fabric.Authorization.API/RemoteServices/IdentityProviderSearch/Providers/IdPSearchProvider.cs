using System;
using System.Collections.Generic;
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

        public async Task<FabricIdPSearchResponse> Search(IdPPrincipalSearchRequest request)
        {
            var settings = _appConfiguration.IdentityServerConfidentialClientSettings;
            var baseUri = settings.Authority.EnsureTrailingSlash();
            var tokenUriAddress = $"{baseUri}connect/token";
            var tokenClient = new TokenClient(tokenUriAddress, "fabric-authorization-client", settings.ClientSecret);
            var accessTokenResponse = await tokenClient
                .RequestClientCredentialsAsync(IdentityProviderSearchScopes.SearchUsersScope).ConfigureAwait(false);

            // TODO: fix URI path
            var httpRequestMessage = _httpRequestMessageFactory.CreateWithAccessToken(HttpMethod.Get,
                new Uri($"{baseUri}api/users"),
                accessTokenResponse.AccessToken);

            _logger.Debug($"Invoking {ServiceName} endpoint {httpRequestMessage.RequestUri}");

            httpRequestMessage.Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8,
                "application/json");

            return await ProcessResponse<IdPPrincipalSearchResponse, FabricIdPSearchResponse>(httpRequestMessage);
        }

        public async Task<FabricIdPGroupResponse> GetGroup(IdPGroupRequest request)
        {
            var settings = _appConfiguration.IdentityServerConfidentialClientSettings;
            var baseUri = settings.Authority.EnsureTrailingSlash();
            var tokenUriAddress = $"{baseUri}connect/token";
            var tokenClient = new TokenClient(tokenUriAddress, "fabric-authorization-client", settings.ClientSecret);
            var accessTokenResponse = await tokenClient
                .RequestClientCredentialsAsync(IdentityProviderSearchScopes.SearchUsersScope).ConfigureAwait(false);

            // TODO: fix URI path
            var httpRequestMessage = _httpRequestMessageFactory.CreateWithAccessToken(HttpMethod.Get,
                new Uri($"{baseUri}api/{IdentityConstants.AzureActiveDirectory}/tenant/{request.DisplayName}"),
                accessTokenResponse.AccessToken);

            _logger.Debug($"Invoking {ServiceName} endpoint {httpRequestMessage.RequestUri}");

            httpRequestMessage.Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8,
                "application/json");

            return await ProcessResponse<IdPGroupResponse, FabricIdPGroupResponse>(httpRequestMessage);
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
