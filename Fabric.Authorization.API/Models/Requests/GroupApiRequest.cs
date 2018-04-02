namespace Fabric.Authorization.API.Models.Requests
{
    public class GroupIdentifierApiRequest
    {
        public string GroupName { get; set; }
    }

    public class GroupPatchApiRequest
    {
        public string DisplayName { get; set; }
        public string Description { get; set; }
    }
}