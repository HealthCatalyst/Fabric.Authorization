namespace Fabric.Authorization.API.Models
{
    public class PermissionRequestContext
    {
        public string RequestedGrain { get; set; }
        public string RequestedSecurableItem { get; set; }
    }
}