using System.Collections.Generic;

namespace Fabric.Authorization.MetadataFetcher
{
    public interface IGroupFetcher
    {
        IEnumerable<string> FetchAllGroups(Dictionary<string, string> props);
    }
}