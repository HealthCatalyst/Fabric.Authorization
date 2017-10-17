using System;
using System.Threading.Tasks;
using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.API.Models.Search;
using Fabric.Authorization.API.Models.Search.Validators;
using Fabric.Authorization.API.Services;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Nancy;
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
            IPropertySettings propertySettings = null) : base("/v1/identities", logger, validator,
            propertySettings)
        {
            _identitySearchService = identitySearchService;

            Get("/", async _ => await GetIdentities().ConfigureAwait(false), null, "GetIdentities");
        }

        private async Task<dynamic> GetIdentities()
        {
            try
            {
                this.RequiresClaims(AuthorizationReadClaim);
                var searchRequest = this.Bind<IdentitySearchRequest>();
                Validate(searchRequest);
                var authResponse = await _identitySearchService.Search(searchRequest);
                return CreateSuccessfulGetResponse(authResponse.Results, authResponse.HttpStatusCode);
            }
            catch (NotFoundException<Client> ex)
            {
                return CreateFailureResponse(ex.Message, HttpStatusCode.NotFound);
            }
            catch (NotFoundException<Group> ex)
            {
                return CreateFailureResponse(ex.Message, HttpStatusCode.NotFound);
            }
            catch (NotFoundException<Role> ex)
            {
                return CreateFailureResponse(ex.Message, HttpStatusCode.NotFound);
            }
            catch (Exception ex)
            {
                return CreateFailureResponse(ex.Message, HttpStatusCode.InternalServerError);
            }
        }
    }
}