using Fabric.Authorization.API.Configuration;
using Microsoft.AspNetCore.Builder;

namespace Fabric.Authorization.API.Infrastructure.Middleware
{
    public static class AngularMiddlewareExtensions
    {
        public static IApplicationBuilder UseAngular(this IApplicationBuilder builder, IAppConfiguration appConfiguration)
        {
            return builder.UseMiddleware<AngularMiddleware>(appConfiguration);
        }
    }
}
