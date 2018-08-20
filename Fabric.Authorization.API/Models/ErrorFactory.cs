using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using Nancy;
using Catalyst.Fabric.Authorization.Models;

namespace Fabric.Authorization.API.Models
{
    public static class ErrorFactory
    {

        public static Error CreateError<T>(ValidationResult validationResult, HttpStatusCode statusCode)
        {
            var error = validationResult.ToError();
            error.Code = Enum.GetName(typeof(HttpStatusCode), statusCode);
            error.Target = typeof(T).Name;
            return error;
        }

        public static Error CreateError<T>(string message, HttpStatusCode statusCode)
        {
            var error = new Error
            {
                Code = Enum.GetName(typeof(HttpStatusCode), statusCode),
                Target = typeof(T).Name,
                Message = message
            };
            return error;
        }

        public static Error CreateError<T>(IEnumerable<string> messages, HttpStatusCode statusCode)
        {
            return messages.ToError(typeof(T).Name, statusCode);
        }

        public static Error ToError(this IEnumerable<string> errors, string target, HttpStatusCode statusCode)
        {
            var details = errors.Select(e => new Error
            {
                Code = statusCode.ToString(),
                Message = e,
                Target = target
            }).ToList();

            var error = new Error
            {
                Message = details.Count > 1 ? "Multiple Errors" : details.FirstOrDefault()?.Message,
                Details = details.ToArray()
            };

            return error;
        }
    }
}
