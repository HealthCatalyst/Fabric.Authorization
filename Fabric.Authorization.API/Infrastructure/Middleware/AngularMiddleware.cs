using System.IO;
using System.Net;
using System.Threading.Tasks;
using Fabric.Authorization.API.Constants;
using Microsoft.AspNetCore.Http;

namespace Fabric.Authorization.API.Infrastructure.Middleware
{
    public class AngularMiddleware
    {
        private readonly RequestDelegate _next;

        public AngularMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            await _next(context);
            var accessControlPath = $"/{AccessControl.Path}/";
            if (context.Response.StatusCode ==  (int)HttpStatusCode.NotFound &&
                !Path.HasExtension(context.Request.Path.Value) &&
                context.Request.Path.Value.StartsWith(accessControlPath))
            {
                context.Request.Path = $"{accessControlPath}{AccessControl.Index}";
                await _next(context);
            }
        }
    }
}
