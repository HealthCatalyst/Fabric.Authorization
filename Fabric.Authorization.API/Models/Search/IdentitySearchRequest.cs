using Newtonsoft.Json;

namespace Fabric.Authorization.API.Models.Search
{
    public class IdentitySearchRequest : SearchRequest
    {
        public string ClientId { get; set; }
    }
}