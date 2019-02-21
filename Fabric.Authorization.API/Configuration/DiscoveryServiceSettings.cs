namespace Fabric.Authorization.API.Configuration
{
    public class DiscoveryServiceSettings
    {
        public string DiscoveryServiceToken { get; set; }
        public string IdentityServiceToken { get; set; }
        public string AccessControlUIToken { get; set; }
        public bool UseDiscovery { get; set; }
        public bool UseOAuth2Authentication { get; set; }
        public string Endpoint { get; set; }
    }
}
