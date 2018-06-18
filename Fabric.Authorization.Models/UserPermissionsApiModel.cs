using System.Collections.Generic;

namespace Fabric.Authorization.Models
{
    public class UserPermissionsApiModel : ApiModelBase
    {
        public IEnumerable<string> Permissions { get; set; }
    }
}
