using System.Collections.Generic;

namespace Fabric.Authorization.API.Models
{
    public class ApiModelBase
    {
        public IEnumerable<PermissionRequestContext> PermissionRequestContexts { get; set; }
    }
}
