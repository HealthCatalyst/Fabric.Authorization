using System.Collections.Generic;
using System.Linq;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores.Services;

namespace Fabric.Authorization.Domain.Resolvers.Permissions
{
    public class EffectivePermissionResolver : DetailedPermissionResolver
    {
        public EffectivePermissionResolver(
            UserService userService,
            GroupService groupService,
            RoleService roleService) : base(userService, groupService, roleService)
        {
        }

        public override IEnumerable<Permission> Resolve(PermissionResolutionRequest resolutionRequest)
        {
            return AllowedPermissions.Except(DeniedPermissions).Distinct();
        }
    }
}