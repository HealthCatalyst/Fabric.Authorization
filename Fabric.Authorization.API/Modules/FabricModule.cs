using System;
using Fabric.Authorization.API.Models;
using FluentValidation.Results;
using Nancy;
using Nancy.Responses.Negotiation;
using Fabric.Authorization.API.Constants;

namespace Fabric.Authorization.API.Modules
{
    public abstract class FabricModule : NancyModule
    {
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

        protected Negotiator CreateFailureResponse(ValidationResult validationResult)
        {
            return Negotiate.WithModel(validationResult).WithStatusCode(HttpStatusCode.BadRequest);
        }
    }
}
