using System.Collections.Generic;

namespace Fabric.Authorization.API.Models
{
    public class UserPermissionsApiModel : ApiModelBase
    {
        public IEnumerable<string> Permissions { get; set; }
    }
}
