using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Platform.Shared.Configuration.Docker;
using Microsoft.Extensions.Configuration;

namespace Fabric.Authorization.API.Configuration
{
    public class AuthorizationConfigurationProvider
    {
        public IAppConfiguration GetAppConfiguration(string basePath)
        {
            return BuildAppConfiguration(basePath);
        }

        private IAppConfiguration BuildAppConfiguration(string baseBath)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .AddDockerSecrets(typeof(IAppConfiguration))
                .SetBasePath(baseBath)
                .Build();

            var appConfig = new AppConfiguration();
            ConfigurationBinder.Bind(config, appConfig);
            return appConfig;
        }
    }
}
