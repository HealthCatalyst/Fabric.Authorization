using Fabric.Authorization.API.Configuration;
using Microsoft.AspNetCore.Hosting;

namespace Fabric.Authorization.API.Extensions
{
    public static class WebHostBuilderExtensions
    {
        public static IWebHostBuilder UseIisIntegrationIfConfigured(this IWebHostBuilder builder,
            IAppConfiguration appConfiguration)
        {
            if (appConfiguration.HostingOptions != null && appConfiguration.HostingOptions.UseIis)
                builder.UseIISIntegration();
            return builder;
        }
    }
}