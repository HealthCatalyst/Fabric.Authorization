using System.Collections.Generic;

namespace Fabric.Authorization.API.Models
{
    public class UserPermissionsResponse : ResponseBase
    {
        public IEnumerable<string> Permissions { get; set; }
    }
}
