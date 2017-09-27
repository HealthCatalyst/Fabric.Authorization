using System.Collections.Generic;

namespace Fabric.Authorization.API.RemoteServices.Identity.Models
{
    public class UserSearchRequest
    {
        public string ClientId { get; set; }
        public IEnumerable<string> UserIds { get; set; }
    }
}