using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.API.Models.Search;
using Fabric.Authorization.API.Models.Search.Validators;
using Fabric.Authorization.API.Services;
using Serilog;

namespace Fabric.Authorization.API.Modules
{
    public class IdentitySearchModule : SearchModule<IdentitySearchRequest>
    {
        public IdentitySearchModule(
            IdentitySearchService searchService,
            IdentitySearchRequestValidator validator,
            ILogger logger,
            IPropertySettings propertySettings = null) : base("/v1/search/identities", logger, validator,
            propertySettings)
        {
        }
    }
}