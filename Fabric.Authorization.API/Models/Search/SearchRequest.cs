using Newtonsoft.Json;

namespace Fabric.Authorization.API.Models.Search
{
    public abstract class SearchRequest
    {
        [JsonProperty("page_number")]
        public int PageNumber { get; set; }

        [JsonProperty("page_size")]
        public int PageSize { get; set; }

        public string Filter { get; set; }

        [JsonProperty("sort_key")]
        public string SortKey { get; set; }

        [JsonProperty("sort_dir")]
        public string SortDirection { get; set; }
    }
}