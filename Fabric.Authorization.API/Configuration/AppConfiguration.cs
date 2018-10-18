using Fabric.Authorization.Persistence.SqlServer.Configuration;
using Fabric.Platform.Shared.Configuration;

namespace Fabric.Authorization.API.Configuration
{
    public class AppConfiguration : IAppConfiguration
    {
        public string ClientName { get; set; }
        public string StorageProvider { get; set; }
        public string AuthorizationAdmin { get; set; }
        public ElasticSearchSettings ElasticSearchSettings { get; set; }
        public IdentityServerConfidentialClientSettings IdentityServerConfidentialClientSettings { get; set; }
        public ApplicationInsights ApplicationInsights { get; set; }
        public HostingOptions HostingOptions { get; set; }
        public EncryptionCertificateSettings EncryptionCertificateSettings { get; set; }
        public DefaultPropertySettings DefaultPropertySettings{ get; set; }
        public ConnectionStrings ConnectionStrings { get; set; }
        public EntityFrameworkSettings EntityFrameworkSettings { get; set; }
        public string ApplicationEndpoint { get; set; }
        public AccessControlSettings AccessControlSettings { get; set; }
        public string DiscoveryServiceEndpoint { get; set; }
        public IdentityProviderSearchSettings IdentityProviderSearchSettings { get; set; }
        public bool UseAzureAuthentication { get; set; }
    }
}
