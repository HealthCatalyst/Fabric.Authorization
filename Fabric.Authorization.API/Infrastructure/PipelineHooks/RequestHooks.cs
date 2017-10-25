using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

using Fabric.Authorization.API.Models;

using Nancy;

using HttpStatusCode = Nancy.HttpStatusCode;

namespace Fabric.Authorization.API.Infrastructure.PipelineHooks
{
    public static class RequestHooks
    {
        private static readonly Regex VersionRegex = new Regex("\\/v\\d(.\\d)?\\/");

        public static readonly Func<NancyContext, Response> SetDefaultVersionInUrl = context =>
        {
            var url = context.Request.Url;
            var versionInUrlMatch = VersionRegex.Match(url);

            if (versionInUrlMatch.Success)
            {
                //a version exists so do nothing with url
                return null;
            }

            //modify the url, default to the first version of the api (v1)
            var originalRequest = context.Request;
            var siteBase = url.SiteBase;
            var path = WebUtility.UrlEncode(url.Path);

            var version1Url = $"{siteBase}/v1{path}{url.Query}";

            var headers = originalRequest.Headers.ToDictionary(originalRequestHeader => originalRequestHeader.Key,
                originalRequestHeader => originalRequestHeader.Value);

            var updatedRequest = new Request(
                originalRequest.Method,
                version1Url,
                originalRequest.Body,
                headers,
                originalRequest.UserHostAddress,
                originalRequest.ClientCertificate,
                originalRequest.ProtocolVersion);

            context.Request = updatedRequest;

            return null;
        };

        public static readonly Func<NancyContext, Response> RemoveContentTypeHeaderForGet = context =>
        {
            //only check GET requests
            if (!context.Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var contentType = context.Request.Headers.ContentType;
            if (contentType == null)
            {
                return null;
            }

            //remove content-type header from the request
            var originalRequest = context.Request;
            var headers = originalRequest.Headers.ToDictionary(originalRequestHeader => originalRequestHeader.Key,
                originalRequestHeader => originalRequestHeader.Value);
            
            headers.Remove("Content-Type");

            var updatedRequest = new Request(
                originalRequest.Method,
                originalRequest.Url,
                originalRequest.Body,
                headers,
                originalRequest.UserHostAddress,
                originalRequest.ClientCertificate,
                originalRequest.ProtocolVersion);

            context.Request = updatedRequest;

            return null;
            
        };
    }
}