using Fabric.Authorization.API.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Fabric.Authorization.API.Infrastructure.Middleware
{
    public static class AngularMiddlewareExtensions
    {
        public static IApplicationBuilder UseAngular(this IApplicationBuilder builder, IAppConfiguration appConfiguration, IHostingEnvironment hostingEnvironment)
        {
            return builder.UseMiddleware<AngularMiddleware>(appConfiguration, hostingEnvironment);
        }
    }
}
