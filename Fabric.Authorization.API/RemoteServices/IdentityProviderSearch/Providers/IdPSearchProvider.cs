using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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
        private const string PrincipalsEndpoint = "principals/";

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

        public async Task<FabricIdPGroupResponse> GetGroupAsync(IdPGroupRequest request)
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
            var idPGroupResponse = new IdPGroupResponse();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                _logger.Error(
                    $"Response status code from {ServiceName} {httpRequestMessage.RequestUri} => {response.StatusCode}");
                _logger.Error(
                    $"Response content from {ServiceName} {httpRequestMessage.RequestUri} => {responseContent}");
            }
            else
            {
                idPGroupResponse = JsonConvert.DeserializeObject<IdPGroupResponse>(responseContent);
                _logger.Debug($"{ServiceName} {httpRequestMessage.RequestUri} result: {idPGroupResponse}");
            }

            return new FabricIdPGroupResponse
            {
                HttpStatusCode = response.StatusCode,
                Results = idPGroupResponse.Principals
            };
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
                new Uri($"{baseUri}{route}"),
                accessTokenResponse.AccessToken);

            httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return httpRequestMessage;
        }
    }
}
