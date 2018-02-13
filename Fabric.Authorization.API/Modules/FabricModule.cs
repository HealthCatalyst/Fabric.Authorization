using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.API.ModuleExtensions;
using Fabric.Authorization.API.Services;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using FluentValidation;
using Nancy;
using Nancy.Extensions;
using Nancy.Responses.Negotiation;
using Nancy.Security;
using Serilog;

namespace Fabric.Authorization.API.Modules
{
    public abstract class FabricModule<T> : NancyModule
    {
        protected ILogger Logger;
        protected AbstractValidator<T> Validator;
        protected readonly AccessService AccessService;

        protected FabricModule()
        {
        }

        protected FabricModule(
            string path,
            ILogger logger,
            AbstractValidator<T> abstractValidator,
            AccessService accessService,
            IPropertySettings propertySettings = null) : base(path)
        {
            Validator = abstractValidator ?? throw new ArgumentNullException(nameof(abstractValidator));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            PropertySettings = propertySettings;
            AccessService = accessService ?? throw new ArgumentNullException(nameof(accessService));
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

        public Negotiator CreateFailureResponse(string message, HttpStatusCode statusCode)
        {
            var error = ErrorFactory.CreateError<T>(message, statusCode);
            return Negotiate.WithModel(error).WithStatusCode(statusCode);
        }

        protected Negotiator CreateFailureResponse(IEnumerable<string> messages, HttpStatusCode statusCode)
        {
            var error = ErrorFactory.CreateError<T>(messages, statusCode);
            return Negotiate.WithModel(error).WithStatusCode(statusCode);
        }

        protected async Task CheckWriteAccess(ClientService clientService,
            dynamic grain,
            dynamic securableItem)
        {
            string grainAsString = grain.ToString();
            string securableItemAsString = securableItem.ToString();

            if (HasSubjectId)
            {
                try
                {
                    var requiredClaims = await GetRequiredClaims(clientService, grainAsString);
                    await AccessService.CheckUserAccess(grainAsString, securableItemAsString, this,
                        requiredClaims.ToArray());
                }
                catch (NotFoundException<Grain>)
                {
                    this.AddBeforeHookOrExecute((context) => AccessService.CreateFailuerResponse<Grain>(
                        $"The requested grain: {grainAsString} does not exist.", context, HttpStatusCode.BadRequest));
                }
            }
            else
            {
                await AccessService.CheckAppAccess(ClientId, grainAsString, securableItemAsString, clientService, this, AuthorizationWriteClaim);
            }
        }

        protected void CheckReadAccess()
        {
            this.RequiresClaims(AuthorizationReadClaim);
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

        public string SubjectId => Context.CurrentUser.Claims.First(c => c.Type == Claims.Sub).Value;

        public string IdentityProvider => Context.CurrentUser.Claims.First(c => c.Type == Claims.IdentityProvider).Value;

        public bool HasSubjectId => Context.CurrentUser.HasClaim(c => c.Type == Claims.Sub);

        protected Predicate<Claim> GetClientIdPredicate(string clientId)
        {
            return claim => claim.Type == Claims.ClientId && claim.Value == clientId;
        }

        private async  Task<List<Predicate<Claim>>> GetRequiredClaims(ClientService clientService, string grain)
        {
            var grainModel = await clientService.GetGrain(grain);
            var requiredClaims = new List<Predicate<Claim>> { AuthorizationWriteClaim };
            if (grainModel.IsShared && grainModel.RequiredWriteScopes.Count > 0)
            {
                requiredClaims.Add(c => grainModel.RequiredWriteScopes.Contains(c.Value));
            }
            return requiredClaims;
        }
    }
}