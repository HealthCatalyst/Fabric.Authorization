using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Resolvers.Permissions;
using Fabric.Authorization.Domain.Stores.Services;
using Serilog;

namespace Fabric.Authorization.Domain.Resolvers.Models
{
    public class GranularPermissionResolver : IPermissionResolver
    {
        private readonly PermissionService _permissionService;
        private readonly ILogger _logger;

        public GranularPermissionResolver(PermissionService permissionService, ILogger logger)
        {
            _permissionService = permissionService;
            _logger = logger;
        }

        public async Task<PermissionResolutionResult> Resolve(PermissionResolutionRequest resolutionRequest)
        {
            var subjectId = resolutionRequest.SubjectId;
            var identityProvider = resolutionRequest.IdentityProvider;

            if (string.IsNullOrWhiteSpace(subjectId) || string.IsNullOrWhiteSpace(identityProvider))
            {
                _logger.Debug($"Attempted to resolve granular permissions without a complete user ID (subjectId = {subjectId}, identityProvider = {identityProvider}");
                return new PermissionResolutionResult();
            }
            try
            {
                var granularPermissions =
                    await _permissionService.GetUserGranularPermissions($"{subjectId}:{identityProvider}");

                return new PermissionResolutionResult
                {
                    AllowedPermissions = granularPermissions.AdditionalPermissions?.Select(p => p.ToResolvedPermission(ResolvedPermission.Allow)),
                    DeniedPermissions = granularPermissions.DeniedPermissions?.Select(p => p.ToResolvedPermission(ResolvedPermission.Deny))
                };
            }
            catch (NotFoundException<GranularPermission>)
            {
                _logger.Debug($"No granular permissions found for user {subjectId}:{identityProvider}");
            }

            return new PermissionResolutionResult();
        }
    }
}