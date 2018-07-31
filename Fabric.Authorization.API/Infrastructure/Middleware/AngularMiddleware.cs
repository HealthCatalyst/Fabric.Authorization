using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.API.Constants;
using Microsoft.AspNetCore.Http;

namespace Fabric.Authorization.API.Infrastructure.Middleware
{
    public class AngularMiddleware
    {
        private readonly RequestDelegate _next;
        private static string _indexContent;
        private readonly IAppConfiguration _appConfiguration;

        public AngularMiddleware(RequestDelegate next, IAppConfiguration appConfiguration)
        {
            _next = next;
            _appConfiguration = appConfiguration;
        }

        public async Task Invoke(HttpContext context)
        {
            var accessControlPath = $"/{AccessControl.Path}/";
            var indexPath = $"{accessControlPath}{AccessControl.Index}";

            // load index.html into memory
            // replace tokens and cache in _indexContent
            // modify response body (make sure RootModule uses this modified response)
            if (string.IsNullOrEmpty(_indexContent))
            {
                var httpClient = new HttpClient();
                var result = await httpClient.GetAsync(indexPath);
                _indexContent = await result.Content.ReadAsStringAsync();
                var discoveryServiceSettings = _appConfiguration.AccessControlSettings.DiscoveryServiceSettings;
                _indexContent = _indexContent.Replace(discoveryServiceSettings.Token,
                    discoveryServiceSettings.Value);
            }

            await _next(context);
            
            if (context.Response.StatusCode == (int) HttpStatusCode.NotFound &&
                !Path.HasExtension(context.Request.Path.Value) &&
                context.Request.Path.Value.StartsWith(accessControlPath))
            {
                context.Request.Path = indexPath;
                using (var ms = new MemoryStream(Encoding.ASCII.GetBytes(_indexContent)))
                {
                    context.Response.Body = ms;
                }
                 
                await _next(context);
            }
        }
    }
}
