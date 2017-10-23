using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Resolvers.Models;
using Fabric.Authorization.Domain.Stores.Services;
using Serilog;

namespace Fabric.Authorization.Domain.Resolvers.Permissions
{
    public class PermissionResolverService : IPermissionResolverService
    {
        protected readonly PermissionService PermissionService;
        protected readonly RoleService RoleService;
        protected readonly ILogger Logger;
        protected readonly IEnumerable<IPermissionResolverService> PermissionResolverServices;

        public PermissionResolverService(
            RoleService roleService,
            PermissionService permissionService,
            IEnumerable<IPermissionResolverService> permissionResolverServices,
            ILogger logger)
        {
            RoleService = roleService;
            PermissionService = permissionService;
            Logger = logger;
            PermissionResolverServices = permissionResolverServices;
        }

        public async Task<PermissionResolutionResult> Resolve(PermissionResolutionRequest resolutionRequest)
        {
            var allowedPermissions = new List<ResolvedPermission>();
            var deniedPermissions = new List<ResolvedPermission>();

            foreach (var permissionResolverService in PermissionResolverServices)
            {
                var permissionResolutionResult = await permissionResolverService.Resolve(resolutionRequest);
                allowedPermissions.AddRange(permissionResolutionResult.AllowedPermissions);
                deniedPermissions.AddRange(permissionResolutionResult.DeniedPermissions);
            }

            return new PermissionResolutionResult
            {
                AllowedPermissions = allowedPermissions,
                DeniedPermissions = deniedPermissions
            };
        }
    }
}