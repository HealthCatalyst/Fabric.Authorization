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

        protected Negotiator CreateFailureResponse<T>(ValidationResult validationResult, HttpStatusCode statusCode)
        {
            var error = validationResult.ToError();
            error.Code = Enum.GetName(typeof(HttpStatusCode), statusCode);
            error.Target = typeof(T).Name;
            return Negotiate.WithModel(error).WithStatusCode(statusCode);
        }

        protected Negotiator CreateFailureResponse<T>(string message, HttpStatusCode statusCode)
        {
            var error = new Error
            {
                Code = Enum.GetName(typeof(HttpStatusCode), statusCode),
                Target = typeof(T).Name,
                Message = message
            };
            return Negotiate.WithModel(error).WithStatusCode(statusCode);
        }
    }
}
