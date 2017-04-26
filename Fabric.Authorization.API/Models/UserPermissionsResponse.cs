using System.Collections.Generic;

namespace Fabric.Authorization.API.Models
{
    public class UserPermissionsResponse
    {
        public string UserId { get; set; }
        public string RequestedGrain { get; set; }
        public string RequestedResource { get; set; }
        public IEnumerable<string> Permissions { get; set; }
    }
}
