using System.Collections.Generic;

namespace Catalyst.Fabric.Authorization.Models
{
    public class UserPermissionsApiModel : ApiModelBase
    {
        public IEnumerable<string> Permissions { get; set; }
    }
}
