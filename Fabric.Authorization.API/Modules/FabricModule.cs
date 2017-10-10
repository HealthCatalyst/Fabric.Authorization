using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.API.ModuleExtensions;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores.Services;
using FluentValidation;
using Nancy;
using Nancy.Responses.Negotiation;
using Serilog;

namespace Fabric.Authorization.API.Modules
{
    public abstract class FabricModule<T> : NancyModule
    {
        protected ILogger Logger;
        protected AbstractValidator<T> Validator;

        protected FabricModule()
        {
        }

        protected FabricModule(
            string path,
            ILogger logger,
            AbstractValidator<T> abstractValidator,
            IPropertySettings propertySettings = null) : base(path)
        {
            Validator = abstractValidator ?? throw new ArgumentNullException(nameof(abstractValidator));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            PropertySettings = propertySettings;
        }

        protected IPropertySettings PropertySettings { get; set; }

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

        protected string ClientId => Context.CurrentUser?.FindFirst(Claims.ClientId)?.Value;

        protected Negotiator CreateSuccessfulPostResponse(IIdentifiable model,
            HttpStatusCode statusCode = HttpStatusCode.Created)
        {
            var uriBuilder = new UriBuilder(Request.Url.Scheme,
                Request.Url.HostName,
                Request.Url.Port ?? 80,
                $"{ModulePath}/{model.Identifier}");

            var selfLink = uriBuilder.ToString();

            return Negotiate
                .WithModel(model)
                .WithStatusCode(statusCode)
                .WithHeader(HttpResponseHeaders.Location, selfLink);
        }

        protected Negotiator CreateSuccessfulGetResponse<T1>(T1 model, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            return Negotiate
                .WithModel(model)
                .WithStatusCode(statusCode);
        }

        protected Negotiator CreateFailureResponse(string message, HttpStatusCode statusCode)
        {
            var error = ErrorFactory.CreateError<T>(message, statusCode);
            return Negotiate.WithModel(error).WithStatusCode(statusCode);
        }

        protected async Task CheckAccess(ClientService clientService, dynamic grain, dynamic securableItem,
            params Predicate<Claim>[] requiredClaims)
        {
            string grainAsString = grain.ToString();
            string securableItemAsString = securableItem.ToString();
            var doesClientOwnItem = false;

            try
            {
                doesClientOwnItem =
                    await clientService.DoesClientOwnItem(ClientId, grainAsString, securableItemAsString);
            }
            catch (NotFoundException<Client>)
            {
                doesClientOwnItem = false;
            }

            this.RequiresOwnershipAndClaims<T>(doesClientOwnItem, grainAsString, securableItemAsString, requiredClaims);
        }

        protected void Validate(T model)
        {
            var validationResults = Validator.Validate(model);
            if (!validationResults.IsValid)
            {
                Logger.Information("Validation failed for model: {@model}. ValidationResults: {@validationResults}.",
                    model, validationResults);
                this.CreateValidationFailureResponse<T>(validationResults);
            }
        }

        protected string SubjectId => Context.CurrentUser.Claims.First(c => c.Type == Claims.Sub).Value;
        protected string IdentityProvider => Context.CurrentUser.Claims.First(c => c.Type == Claims.IdentityProvider).Value;

        protected Predicate<Claim> GetClientIdPredicate(string clientId)
        {
            return claim => claim.Type == Claims.ClientId && claim.Value == clientId;
        }
    }
}