using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Resolvers.Models;
using Fabric.Authorization.Domain.Stores.Services;
using Serilog;

namespace Fabric.Authorization.Domain.Resolvers.Permissions
{
    public class PermissionResolver : IPermissionResolver
    {
        protected readonly PermissionService PermissionService;
        protected readonly RoleService RoleService;
        protected readonly ILogger Logger;

        public PermissionResolver(
            RoleService roleService,
            PermissionService permissionService,
            ILogger logger)
        {
            RoleService = roleService;
            PermissionService = permissionService;
            Logger = logger;
        }

        public async Task<PermissionResolutionResult> Resolve(PermissionResolutionRequest resolutionRequest)
        {
            var granularPermissions = await new GranularPermissionResolver(PermissionService, Logger).Resolve(resolutionRequest);
            var rolePermissions = await new RolePermissionResolver(RoleService).Resolve(resolutionRequest);

            return new PermissionResolutionResult
            {
                AllowedPermissions = granularPermissions.AllowedPermissions.Concat(rolePermissions.AllowedPermissions),
                DeniedPermissions = granularPermissions.DeniedPermissions.Concat(rolePermissions.DeniedPermissions)
            };
        }
    }
}