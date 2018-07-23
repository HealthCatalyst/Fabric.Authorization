using System.Collections.Generic;

namespace Catalyst.Fabric.Authorization.Models
{
    public class ResolvedPermissionApiModel : PermissionApiModel
    {
        public IEnumerable<PermissionRoleApiModel> Roles { get; set; }
    }
}
