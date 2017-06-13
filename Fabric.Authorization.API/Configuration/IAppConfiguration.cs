using Fabric.Platform.Shared.Configuration;

namespace Fabric.Authorization.API.Configuration
{
    public interface IAppConfiguration
    {
        string ClientName { get; }
        ElasticSearchSettings ElasticSearchSettings { get; }
        IdentityServerConfidentialClientSettings IdentityServerConfidentialClientSettings { get; }
    }
}
