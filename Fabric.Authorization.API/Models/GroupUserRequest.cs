namespace Fabric.Authorization.API.Models
{
    public class GroupUserRequest
    {
        public string GroupName { get; set; }
        public string SubjectId { get; set; }
        public string IdentityProvider { get; set; }
    }
}