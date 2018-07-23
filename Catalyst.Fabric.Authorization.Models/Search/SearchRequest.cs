namespace Catalyst.Fabric.Authorization.Models.Search
{
    public abstract class SearchRequest
    {
        public int? PageNumber { get; set; }
        public int? PageSize { get; set; }
        public string Filter { get; set; }
        public string SortKey { get; set; }
        public string SortDirection { get; set; }
    }
}