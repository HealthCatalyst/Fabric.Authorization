using System.Threading.Tasks;
using Fabric.Authorization.API.RemoteServices.Discovery;

namespace Fabric.Authorization.API.Configuration
{
    /// <summary>
    /// Extension class for IAppConfiguration extensions.
    /// </summary>
    public static class AppConfigurationExtensions
    {
        /// <summary>
        /// Configures the IdentityProviderSearchService Url in IAppConfiguration.
        /// </summary>
        /// <param name="appConfig">The <see cref="IAppConfiguration"/> instance to configure.</param>
        public static void ConfigureIdentityProviderSearchServiceUrl(this IAppConfiguration appConfig)
        {
            var discoveryServiceSettings = appConfig.DiscoveryServiceSettings;
            if (!discoveryServiceSettings.UseDiscovery)
            {
                return;
            }

            using (var discoveryServiceClient = new DiscoveryServiceClient(discoveryServiceSettings.Endpoint))
            {
                var identityProviderSearchServiceRegistration = Task
                    .Run(() => discoveryServiceClient.GetServiceAsync("IdentityProviderSearchService", 1))
                    .Result;

                if (!string.IsNullOrEmpty(identityProviderSearchServiceRegistration?.ServiceUrl))
                {
                    appConfig.IdentityProviderSearchSettings.Endpoint =
                        identityProviderSearchServiceRegistration.ServiceUrl;
                }
            }
        }
    }
}
