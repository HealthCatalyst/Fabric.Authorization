namespace Catalyst.Fabric.Authorization.Models
{
    public class GroupUserRequest
    {
        public string GroupName { get; set; }
        public string TenantId { get; set; }
        public string GroupIdentityProvider { get; set; }
        public string SubjectId { get; set; }
        public string IdentityProvider { get; set; }
    }
}