using System.Threading.Tasks;
using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.API.Models.Search;
using Fabric.Authorization.API.Models.Search.Validators;
using Fabric.Authorization.API.Services;
using Nancy.ModelBinding;
using Nancy.Security;
using Serilog;

namespace Fabric.Authorization.API.Modules
{
    public class IdentitySearchModule : SearchModule<IdentitySearchRequest>
    {
        private readonly IdentitySearchService _identitySearchService;

        public IdentitySearchModule(
            IdentitySearchService identitySearchService,
            IdentitySearchRequestValidator validator,
            ILogger logger,
            IPropertySettings propertySettings = null) : base("/v1/search/identities", logger, validator,
            propertySettings)
        {
            _identitySearchService = identitySearchService;

            Get("/", async _ => await SearchIdentities().ConfigureAwait(false), null, "SearchIdentities");
        }

        private async Task<dynamic> SearchIdentities()
        {
            this.RequiresClaims(AuthorizationReadClaim);
            var searchRequest = this.Bind<IdentitySearchRequest>();
            Validate(searchRequest);
            var results = await _identitySearchService.Search(searchRequest);
            return results;
        }
    }
}