namespace Fabric.Authorization.API.RemoteServices.IdentityProviderSearch.Models
{
    public class IdPGroupRequest
    {
        public string IdentityProvider { get; set; }
        public string TenantId { get; set; }
        public string DisplayName { get; set; }
    }
}
