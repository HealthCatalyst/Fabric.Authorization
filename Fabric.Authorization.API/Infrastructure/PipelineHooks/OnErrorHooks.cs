using System;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Models;
using Nancy;
using Nancy.Responses.Negotiation;
using Serilog;

namespace Fabric.Authorization.API.Infrastructure.PipelineHooks
{
    public class OnErrorHooks
    {
        private readonly ILogger _logger;

        public OnErrorHooks(ILogger logger)
        {
            _logger = logger;
        }

        internal dynamic HandleInternalServerError(NancyContext context, Exception exception,
            IResponseNegotiator responseNegotiator)
        {
            _logger.Error(exception, "Unhandled error on request: @{Url}. Error Message: @{Message}", context.Request.Url,
                exception.Message);

            context.NegotiationContext = new NegotiationContext();

            var negotiator = new Negotiator(context)
                .WithStatusCode(HttpStatusCode.InternalServerError)
                .WithModel(new Error()
                {
                    Message = "There was an internal server error while processing the request.",
                    Code = ((int)HttpStatusCode.InternalServerError).ToString()
                })
                .WithHeaders(HttpResponseHeaders.CorsHeaders);


            var response = responseNegotiator.NegotiateResponse(negotiator, context);
            return response;
        }
    }
}
