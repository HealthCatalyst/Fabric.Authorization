using System.Threading.Tasks;
using ModelHttpStatus = Catalyst.Fabric.Authorization.Models.Enums.HttpStatusCode;
using Catalyst.Fabric.Authorization.Models.Search;
using Fabric.Authorization.API.Configuration;
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
    public class MemberSearchModule : SearchModule<MemberSearchRequest>
    {
        private readonly MemberSearchService _memberSearchService;

        public MemberSearchModule(
            MemberSearchService memberSearchService,
            MemberSearchRequestValidator validator,
            ILogger logger,
            AccessService accessService,
            IPropertySettings propertySettings = null) : base("/v1/members", logger, validator, accessService,
            propertySettings)
        {
            _memberSearchService = memberSearchService;

            Get("/", async _ => await GetMembers().ConfigureAwait(false), null, "GetMembers");
        }

        private async Task<dynamic> GetMembers()
        {
            try
            {
                this.RequiresClaims(AuthorizationReadClaim);
                var searchRequest = this.Bind<MemberSearchRequest>();
                Validate(searchRequest);
                var authResponse = await _memberSearchService.Search(searchRequest);
                return CreateSuccessfulGetResponse(authResponse.ToMemberSearchResponseApiModel(),
                    authResponse.HttpStatusCode == ModelHttpStatus.PartialContent
                        ? HttpStatusCode.PaymentRequired
                        : HttpStatusCode.OK);
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
        }
    }
}