using System.Collections.Generic;

namespace Fabric.Authorization.Models
{
    public class ApiModelBase
    {
        public IEnumerable<PermissionRequestContext> PermissionRequestContexts { get; set; }
    }
}
