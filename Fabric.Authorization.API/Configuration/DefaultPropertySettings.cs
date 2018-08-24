namespace Fabric.Authorization.API.Configuration
{
    public class DefaultPropertySettings : IPropertySettings
    {
        public string GroupSource { get; set; }
        public bool DualStoreEDWAdminPermissions { get; set; }
    }
}