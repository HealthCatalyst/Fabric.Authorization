using Newtonsoft.Json;

namespace Fabric.Authorization.API.Models.Search
{
    public class IdentitySearchRequest : SearchRequest
    {
        [JsonProperty("client_id")]
        public string ClientId { get; set; }
    }
}