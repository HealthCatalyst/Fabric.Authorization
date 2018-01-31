using System.Collections.Generic;

namespace Fabric.Authorization.API.Models
{
    public class ResolvedPermissionApiModel : PermissionApiModel
    {
        public IEnumerable<PermissionRoleApiModel> Roles { get; set; }
    }
}
