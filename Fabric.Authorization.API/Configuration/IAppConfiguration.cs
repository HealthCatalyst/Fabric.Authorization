using Fabric.Authorization.Persistence.SqlServer.Configuration;
using Fabric.Platform.Shared.Configuration;

namespace Fabric.Authorization.API.Configuration
{
    public interface IAppConfiguration
    {
        string ClientName { get; }
        string StorageProvider { get; }
        ElasticSearchSettings ElasticSearchSettings { get; }
        IdentityServerConfidentialClientSettings IdentityServerConfidentialClientSettings { get; }
        CouchDbSettings CouchDbSettings { get; }
        ApplicationInsights ApplicationInsights { get; }
        HostingOptions HostingOptions { get; }
        EncryptionCertificateSettings EncryptionCertificateSettings { get; }
        DefaultPropertySettings DefaultPropertySettings{ get; }
        ConnectionStrings ConnectionStrings { get; }
    }
}
