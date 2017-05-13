namespace Fabric.Authorization.API.Models
{
    public class GroupInfoRequest
    {
        public string GroupName { get; set; }
        public string Grain { get; set; }
        public string SecurableItem { get; set; }
    }
}
