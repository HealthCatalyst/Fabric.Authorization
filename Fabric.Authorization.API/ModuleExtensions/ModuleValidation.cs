using System;
using System.Linq;

using Fabric.Authorization.API.Models;
using Fabric.Authorization.Domain.Validators;

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
                var statusCode = HttpStatusCode.BadRequest;
                if (validationResult.Errors.Any(e => e.CustomState.Equals(ValidationEnums.ValidationState.Duplicate)))
                {
                    statusCode = HttpStatusCode.Conflict;
                }

                var error = ErrorFactory.CreateError<T>(validationResult, statusCode);
                return new JsonResponse(error, new DefaultJsonSerializer(context.Environment), context.Environment){ StatusCode = statusCode };
            };
        }
    }
}
