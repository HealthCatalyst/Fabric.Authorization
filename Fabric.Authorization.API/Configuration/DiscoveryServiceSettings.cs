namespace Fabric.Authorization.API.Configuration
{
    public class DiscoveryServiceSettings
    {
        public string AccessControlToken { get; set; }
        public bool UseDiscovery { get; set; }
        public string Endpoint { get; set; }
        public string IdentityServiceToken { get; set; }
    }
}
