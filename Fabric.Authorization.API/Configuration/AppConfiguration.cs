using Fabric.Platform.Shared.Configuration;

namespace Fabric.Authorization.API.Configuration
{
    public class AppConfiguration : IAppConfiguration
    {
        public string ClientName { get; set; }
        public ElasticSearchSettings ElasticSearchSettings { get; set; }
        public IdentityServerConfidentialClientSettings IdentityServerConfidentialClientSettings { get; set; }
        public CouchDbSettings CouchDbSettings { get; set; }
    }
}
