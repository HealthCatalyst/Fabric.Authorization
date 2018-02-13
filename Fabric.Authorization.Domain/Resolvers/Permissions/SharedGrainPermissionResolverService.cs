using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Resolvers.Models;
using Fabric.Authorization.Domain.Services;

namespace Fabric.Authorization.Domain.Resolvers.Permissions
{
    public class SharedGrainPermissionResolverService : IPermissionResolverService
    {
        private readonly GrainService _grainService;
        private readonly IEnumerable<IPermissionResolverService> _permissionResolverServices;

        public SharedGrainPermissionResolverService(GrainService grainService, IEnumerable<IPermissionResolverService> permissionResolverServices)
        {
            _grainService = grainService ?? throw new ArgumentNullException(nameof(grainService));
            _permissionResolverServices = permissionResolverServices ?? throw new ArgumentNullException(nameof(permissionResolverServices));
        }

        public async Task<PermissionResolutionResult> Resolve(PermissionResolutionRequest resolutionRequest)
        {
            if (!resolutionRequest.IncludeSharedPermissions)
            {
                return new PermissionResolutionResult();
            }

            var sharedGrains = await _grainService.GetSharedGrains();

            var permissionResolutionResult = new PermissionResolutionResult();
            foreach (var sharedGrain in sharedGrains)
            {
                foreach (var securableItem in sharedGrain.SecurableItems)
                {
                    // the current instance of SharedGrainPermissionResolverService will be in the _permissionResolverServices
                    // collection, but because the PermissionResolutionRequest is being created with IncludeSharedPermissions = false,
                    // infinite recursion will not happen
                    foreach (var permissionResolverService in _permissionResolverServices)
                    {
                        var result = await permissionResolverService.Resolve(new PermissionResolutionRequest
                        {
                            Grain = sharedGrain.Name,
                            SecurableItem = securableItem.Name,
                            IdentityProvider = resolutionRequest.IdentityProvider,
                            SubjectId = resolutionRequest.SubjectId,
                            UserGroups = resolutionRequest.UserGroups,
                            IncludeSharedPermissions = false
                        });

                        permissionResolutionResult.AllowedPermissions =
                            permissionResolutionResult.AllowedPermissions.Concat(result.AllowedPermissions);

                        permissionResolutionResult.DeniedPermissions =
                            permissionResolutionResult.DeniedPermissions.Concat(result.DeniedPermissions);
                    }
                }
            }

            return permissionResolutionResult;
        }
    }
}