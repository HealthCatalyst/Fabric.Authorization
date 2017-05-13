namespace Fabric.Authorization.API.Models
{
    public class UserInfoRequest
    {
        public string Grain { get; set; }
        public string SecurableItem { get; set; }
    }
}
