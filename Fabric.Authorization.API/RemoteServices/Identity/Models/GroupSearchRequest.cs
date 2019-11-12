namespace Fabric.Authorization.API.RemoteServices.Identity.Models
{
    public class GroupSearchRequest
    {
        public string IdentityProvider { get; set; }
        public string TenantId { get; set; }
        public string DisplayName { get; set; }
    }
}
