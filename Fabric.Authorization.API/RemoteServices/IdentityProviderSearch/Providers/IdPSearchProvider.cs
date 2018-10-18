using System.Net.Http;
using System.Threading.Tasks;
using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.API.RemoteServices.IdentityProviderSearch.Models;
using Fabric.Platform.Http;
using Serilog;

namespace Fabric.Authorization.API.RemoteServices.IdentityProviderSearch.Providers
{
    public class IdPSearchProvider : IIdPSearchProvider
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpRequestMessageFactory _httpRequestMessageFactory;
        private readonly IAppConfiguration _appConfiguration;
        private readonly ILogger _logger;

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

        public Task<IdPPrincipalSearchResponse> Search(IdPPrincipalSearchRequest request)
        {

        }
    }
}
