using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Catalyst.Fabric.Authorization.Models;
using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.API.Constants;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Fabric.Authorization.API.Infrastructure.Middleware
{
    public class AngularMiddleware
    {
        private readonly RequestDelegate _next;
        private static string _indexContent;
        private readonly IAppConfiguration _appConfiguration;
        private readonly IHostingEnvironment _hostingEnvironment;
        private const string HtmlContentType = "text/html";

        public AngularMiddleware(RequestDelegate next, IAppConfiguration appConfiguration, IHostingEnvironment hostingEnvironment)
        {
            _next = next;
            _appConfiguration = appConfiguration;
            _hostingEnvironment = hostingEnvironment;
        }

        public async Task Invoke(HttpContext context)
        {
            var accessControlPath = $"/{AccessControl.Path}/";
            var indexPath = $"{accessControlPath}{AccessControl.Index}";

            // replace tokens, cache in _indexContent, and rewrite index.html with replaced tokens
            if (string.IsNullOrEmpty(_indexContent))
            {
                var fullPath = Path.Combine(_hostingEnvironment.WebRootPath, AccessControl.Path, AccessControl.Index);
                _indexContent = File.ReadAllText(fullPath);

                var discoveryServiceSettings = _appConfiguration.DiscoveryServiceSettings;
                var identityServerSettings = _appConfiguration.IdentityServerConfidentialClientSettings;

                // swaps in the discovery service root
                _indexContent =
                    _indexContent.Replace(discoveryServiceSettings.AccessControlToken, discoveryServiceSettings.Endpoint);

                _indexContent = _indexContent.Replace(discoveryServiceSettings.IdentityServiceToken,
                    identityServerSettings.Authority);

                // swaps in the access control root
                _indexContent = _indexContent.Replace(AccessControl.ClientRootToken,
                    FormatBaseAccessControlUri(this._appConfiguration.ApplicationEndpoint));
            }

            if (context.Request.Path.Equals(indexPath, StringComparison.OrdinalIgnoreCase))
            {
                await WriteIndexResponse(context);
            }
            else
            {
                await _next(context);
                if (context.Response.StatusCode == (int)HttpStatusCode.NotFound &&
                    !Path.HasExtension(context.Request.Path.Value) &&
                    context.Request.Path.Value.StartsWith(accessControlPath))
                {
                    context.Request.Path = indexPath;
                    await WriteIndexResponse(context);
                }
            }
        }

        private string FormatBaseAccessControlUri(string baseUrl)
        {
            return $"{baseUrl.EnsureTrailingSlash()}{AccessControl.Path}/";
        }

        private async Task WriteIndexResponse(HttpContext context)
        {
            using (var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(_indexContent)))
            {
                var originalResponse = context.Response.Body;
                try
                {
                    context.Response.Body = memoryStream;
                    context.Response.ContentLength = memoryStream.Length;
                    context.Response.ContentType = HtmlContentType;
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    await memoryStream.CopyToAsync(originalResponse);
                }
                finally
                {
                    context.Response.Body = originalResponse;
                }
            }
        }
    }
}
