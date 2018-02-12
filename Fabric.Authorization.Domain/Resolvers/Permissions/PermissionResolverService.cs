using System.Collections.Generic;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Resolvers.Models;
using Serilog;

namespace Fabric.Authorization.Domain.Resolvers.Permissions
{
    public class PermissionResolverService : IPermissionResolverService
    {
        protected readonly ILogger Logger;
        protected readonly IEnumerable<IPermissionResolverService> PermissionResolverServices;

        public PermissionResolverService(
            IEnumerable<IPermissionResolverService> permissionResolverServices,
            ILogger logger)
        {
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