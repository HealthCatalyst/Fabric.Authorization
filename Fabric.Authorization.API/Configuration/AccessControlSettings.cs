namespace Fabric.Authorization.API.Configuration
{
    public class AccessControlSettings
    {
        public DiscoveryServiceSettings DiscoveryServiceSettings { get; set; }
    }

    public class DiscoveryServiceSettings
    {
        public string Token { get; set; }
        public string Value { get; set; }
    }
}