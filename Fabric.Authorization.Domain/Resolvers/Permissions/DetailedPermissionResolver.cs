using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores.Services;

namespace Fabric.Authorization.Domain.Resolvers.Permissions
{
    public class DetailedPermissionResolver : IPermissionResolver
    {
        protected readonly GroupService GroupService;
        protected readonly RoleService RoleService;
        protected readonly UserService UserService;
        protected readonly PermissionService PermissionService;

        public DetailedPermissionResolver(
            UserService userService,
            GroupService groupService,
            RoleService roleService,
            PermissionService permissionService)
        {
            UserService = userService;
            GroupService = groupService;
            RoleService = roleService;
            PermissionService = permissionService;
        }

        protected IEnumerable<Permission> AllowedPermissions { get; private set; }

        protected IEnumerable<Permission> DeniedPermissions { get; private set; }

        public virtual async Task<IEnumerable<Permission>> Resolve(PermissionResolutionRequest resolutionRequest)
        {
            var subjectId = resolutionRequest.SubjectId;
            var identityProvider = resolutionRequest.IdentityProvider;

            if (!string.IsNullOrWhiteSpace(subjectId) && !string.IsNullOrWhiteSpace(identityProvider))
            {
                var granularPermissions = await PermissionService.GetUserGranularPermissions($"{subjectId}:{identityProvider}");
            }

            return AllowedPermissions.Concat(DeniedPermissions);
        }
    }
}