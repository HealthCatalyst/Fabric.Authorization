using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.API.Extensions;
using Fabric.Authorization.API.Services;
using Microsoft.AspNetCore.Hosting;

namespace Fabric.Authorization.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var appConfig = new Configuration.AuthorizationConfigurationProvider(new WindowsCertificateService()).GetAppConfiguration(Directory.GetCurrentDirectory());

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
