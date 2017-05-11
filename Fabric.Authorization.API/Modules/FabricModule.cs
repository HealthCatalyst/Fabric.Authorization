using System;
using System.Security.Claims;
using Fabric.Authorization.API.Models;
using Nancy;
using Nancy.Responses.Negotiation;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.ModuleExtensions;
using Fabric.Authorization.Domain.Services;
using FluentValidation;
using Serilog;

namespace Fabric.Authorization.API.Modules
{
    public abstract class FabricModule<T> : NancyModule
    {
        protected AbstractValidator<T> Validator;
        protected ILogger Logger;
        protected Predicate<Claim> AuthorizationReadClaim
        {
            get { return claim => claim.Type == Claims.Scope && claim.Value == Scopes.ReadScope; }
        }

        protected Predicate<Claim> AuthorizationWriteClaim
        {
            get { return claim => claim.Type == Claims.Scope && claim.Value == Scopes.WriteScope; }
        }

        protected Predicate<Claim> AuthorizationManageClientsClaim
        {
            get { return claim => claim.Type == Claims.Scope && claim.Value == Scopes.ManageClientsScope; }
        }

        protected string ClientId => Context.CurrentUser?.FindFirst(Claims.ClientId).Value;

        protected FabricModule()
        { }

        protected FabricModule(string path, ILogger logger, AbstractValidator<T> abstractValidator) : base(path)
        {
            Validator = abstractValidator ?? throw new ArgumentNullException(nameof(abstractValidator));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected Negotiator CreateSuccessfulPostResponse(IIdentifiable model)
        {
            var uriBuilder = new UriBuilder(Request.Url.Scheme,
                Request.Url.HostName,
                Request.Url.Port ?? 80,
                $"{ModulePath}/{model.Identifier}");

            var selfLink = uriBuilder.ToString();

            return Negotiate
                .WithModel(model)
                .WithStatusCode(HttpStatusCode.Created)
                .WithHeader(HttpResponseHeaders.Location, selfLink);
        }

        protected Negotiator CreateFailureResponse(string message, HttpStatusCode statusCode)
        {
            var error = ErrorFactory.CreateError<T>(message, statusCode);
            return Negotiate.WithModel(error).WithStatusCode(statusCode);
        }

        protected void CheckAccess(IClientService clientService, dynamic grain, dynamic securableItem,
            params Predicate<Claim>[] requiredClaims)
        {
            string grainAsString = grain.ToString();
            string securableItemAsString = securableItem.ToString();
            this.RequiresOwnershipAndClaims<T>(clientService, grainAsString, securableItemAsString, requiredClaims);
        }

        protected void Validate(T model)
        {
            var validationResults = Validator.Validate(model);
            if (!validationResults.IsValid)
            {
                Logger.Information("Validation failed for model: {@model}. ValidationResults: {@validationResults}.", model, validationResults);
                this.CreateValidationFailureResponse<T>(validationResults);
            }
        }

        protected Predicate<Claim> GetClientIdPredicate(string clientId)
        {
            return claim => claim.Type == Claims.ClientId && claim.Value == clientId;
        }
    }
}
