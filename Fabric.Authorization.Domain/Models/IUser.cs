namespace Fabric.Authorization.Domain.Models
{
    public interface IUser
    {
        string SubjectId { get; set; }
        string IdentityProvider { get; set; }
        string IdentityProviderUserName { get; set; }
    }
}
