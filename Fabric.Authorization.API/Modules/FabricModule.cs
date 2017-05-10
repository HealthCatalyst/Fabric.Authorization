using System;
using System.Security.Claims;
using Fabric.Authorization.API.Models;
using FluentValidation.Results;
using Nancy;
using Nancy.Responses.Negotiation;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Security;
using Fabric.Authorization.Domain.Clients;

namespace Fabric.Authorization.API.Modules
{
    public abstract class FabricModule : NancyModule
    {
        protected string ReadScope => "fabric/authorization.read";
        protected string WriteScope => "fabric/authorization.write";

        protected Predicate<Claim> AuthorizationReadClaim
        {
            get { return claim => claim.Type == "scope" && claim.Value == ReadScope; }
        }

        protected Predicate<Claim> AuthorizationWriteClaim
        {
            get { return claim => claim.Type == "scope" && claim.Value == WriteScope; }
        }

        protected FabricModule()
        { }

        protected FabricModule(string path) : base(path)
        {
            
        }

        protected Negotiator CreateSuccessfulPostResponse(IIdentifiable model)
        {
            var uriBuilder = new UriBuilder(Request.Url.Scheme,
                Request.Url.HostName,
                Request.Url.Port ?? 80,
                $"{ModulePath}/{model.Id}");

            var selfLink = uriBuilder.ToString();

            return Negotiate
                .WithModel(model)
                .WithStatusCode(HttpStatusCode.Created)
                .WithHeader(HttpResponseHeaders.Location, selfLink);
        }

        protected Negotiator CreateFailureResponse<T>(ValidationResult validationResult, HttpStatusCode statusCode)
        {
            var error = ErrorFactory.CreateError<T>(validationResult, statusCode);
            return Negotiate.WithModel(error).WithStatusCode(statusCode);
        }

        protected Negotiator CreateFailureResponse<T>(string message, HttpStatusCode statusCode)
        {
            var error = ErrorFactory.CreateError<T>(message, statusCode);
            return Negotiate.WithModel(error).WithStatusCode(statusCode);
        }

        protected void CheckAccess<T>(IClientService clientService, dynamic grain, dynamic resource,
            params Predicate<Claim>[] requiredClaims)
        {
            string grainAsString = grain.ToString();
            string resourceAsString = resource.ToString();
            this.RequiresResourceOwnershipAndClaims<T>(clientService, grainAsString, resourceAsString, requiredClaims);
        }
    }
}
