using System.Collections.Generic;

namespace Fabric.Authorization.Domain.Resolvers.Models
{
    public class PermissionResolutionResult
    {
        public IEnumerable<ResolvedPermission> AllowedPermissions { get; set; } = new List<ResolvedPermission>();
        public IEnumerable<ResolvedPermission> DeniedPermissions { get; set; } = new List<ResolvedPermission>();
    }
}