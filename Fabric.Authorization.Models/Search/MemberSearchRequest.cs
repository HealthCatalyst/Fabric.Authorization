namespace Fabric.Authorization.Models.Search
{
    public class MemberSearchRequest : SearchRequest
    {
        public string ClientId { get; set; }
        public string Grain { get; set; }
        public string SecurableItem { get; set; }
    }
}