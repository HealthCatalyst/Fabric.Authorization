namespace Catalyst.Fabric.Authorization.Models
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Catalyst.Fabric.Authorization.Models.Enums;

    public static class ModelExtensions
    {
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

        public static string ToString<T>(this IEnumerable<T> list, string separator)
        {
            return string.Join(separator, list);
        }

        public static string EnsureTrailingSlash(this string url)
        {
            return !url.EndsWith("/") ? $"{url}/" : url;
        }
    }
}
