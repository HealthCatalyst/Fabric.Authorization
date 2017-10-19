using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores.Services;

namespace Fabric.Authorization.Domain.Resolvers.Permissions
{
    public class EffectivePermissionResolver : DetailedPermissionResolver
    {
        public EffectivePermissionResolver(
            UserService userService,
            GroupService groupService,
            RoleService roleService,
            PermissionService permissionService) : base(userService, groupService, roleService, permissionService)
        {
        }

        public override async Task<IEnumerable<Permission>> Resolve(PermissionResolutionRequest resolutionRequest)
        {
            var permissions = await base.Resolve(resolutionRequest);
            return AllowedPermissions.Except(DeniedPermissions).Distinct();
        }
    }
}