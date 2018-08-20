﻿namespace Catalyst.Fabric.Authorization.Models
{
    using System.Collections.Generic;

    public static class ModelExtensions
    {
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
