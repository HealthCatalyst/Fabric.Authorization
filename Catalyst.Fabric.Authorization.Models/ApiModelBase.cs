using System.Collections.Generic;

namespace Catalyst.Fabric.Authorization.Models
{
    public class ApiModelBase
    {
        public IEnumerable<PermissionRequestContext> PermissionRequestContexts { get; set; }
    }
}
