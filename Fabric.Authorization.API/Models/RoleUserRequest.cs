namespace Fabric.Authorization.API.Models
{
    public class RoleUserRequest
    {
        public string SubjectId { get; set; }
        public string IdentityProvider { get; set; }
    }
}