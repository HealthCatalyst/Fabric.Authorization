namespace Catalyst.Fabric.Authorization.Models.Requests
{
    public class GroupIdentifierApiRequest
    {
        public string GroupName { get; set; }
        public string TenantId { get; set; }
        public string IdentityProvider { get; set; }
    }

    public class GroupPostApiRequest : GroupPatchApiRequest
    {
        public string GroupName { get; set; }
        public string IdentityProvider { get; set; }
        public string GroupSource { get; set; }
        public string TenantId { get; set; }
    }

    public class GroupPatchApiRequest
    {
        public string DisplayName { get; set; }
        public string Description { get; set; }
    }

    public class GroupSearchApiRequest 
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }
}