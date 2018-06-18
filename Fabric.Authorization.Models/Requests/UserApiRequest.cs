namespace Fabric.Authorization.Models.Requests
{
    public class UserIdentifierApiRequest
    {
        public string IdentityProvider { get; set; }
        public string SubjectId { get; set; }
    }

    // TODO: add other user-related fields we may need in a User request (e.g., Name)
    public class UserApiRequest : UserIdentifierApiRequest
    {
    }
}
