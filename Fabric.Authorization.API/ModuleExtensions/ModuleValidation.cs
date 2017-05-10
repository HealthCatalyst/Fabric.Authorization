using System;
using Fabric.Authorization.API.Models;
using FluentValidation.Results;
using Nancy;
using Nancy.Extensions;
using Nancy.Responses;

namespace Fabric.Authorization.API.ModuleExtensions
{
    public static class ModuleValidation
    {
        public static void CreateValidationFailureResponse<T>(this NancyModule module,
            ValidationResult validationResult)
        {
            module.AddBeforeHookOrExecute(CreateValidationFailureResponse<T>(validationResult));
        }

        public static Func<NancyContext, Response> CreateValidationFailureResponse<T>(ValidationResult validationResult)
        {
            return (context) =>
            {
                var error = ErrorFactory.CreateError<T>(validationResult, HttpStatusCode.BadRequest);
                return new JsonResponse(error, new DefaultJsonSerializer(context.Environment), context.Environment){ StatusCode = HttpStatusCode.BadRequest};
            };
        }
    }
}
