using System.IO;
using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.API.Extensions;
using Fabric.Authorization.API.Services;
using Microsoft.AspNetCore.Hosting;

namespace Fabric.Authorization.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var appConfig =
                new AuthorizationConfigurationProvider(new WindowsCertificateService()).GetAppConfiguration(Directory
                    .GetCurrentDirectory());

            var host = new WebHostBuilder()
                .UseApplicationInsights()
                .UseKestrel()
                .UseUrls("http://*:5004")
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .UseIisIntegrationIfConfigured(appConfig)
                .Build();

            host.Run();
        }
    }
}