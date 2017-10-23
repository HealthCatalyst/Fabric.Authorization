using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Resolvers.Models;
using Fabric.Authorization.Domain.Stores.Services;

namespace Fabric.Authorization.Domain.Resolvers.Permissions
{
    public class RolePermissionResolverService : IPermissionResolverService
    {
        private readonly RoleService _roleService;

        public RolePermissionResolverService(RoleService roleService)
        {
            _roleService = roleService;
        }

        public async Task<PermissionResolutionResult> Resolve(PermissionResolutionRequest resolutionRequest)
        {
            var grain = resolutionRequest.Grain;
            var securableItem = resolutionRequest.SecurableItem;
            var groupNames = resolutionRequest.UserGroups.ToList();

            var roles = (await _roleService.GetRoles(grain, securableItem)).ToList();

            var allowedPermissions = new List<ResolvedPermission>();
            var deniedPermissions = new List<ResolvedPermission>();

            foreach (var role in roles)
            {
                if (!role.Groups.Any(g => groupNames.Contains(g, StringComparer.OrdinalIgnoreCase)) || role.IsDeleted || role.Permissions == null)
                {
                    continue;
                }

                var resolvedPermissions = role.Permissions
                    .Where(
                        p => IsActiveGrainAndSecurableItemMatch(p, grain, securableItem))
                    .Select(p => p.ToResolvedPermission(ResolvedPermission.Allow))
                    .ToList();

                AddResolvedPermissions(allowedPermissions, resolvedPermissions, role);

                resolvedPermissions = role.DeniedPermissions
                    .Select(p =>
                        p.ToResolvedPermission(ResolvedPermission.Deny))
                    .ToList();

                AddResolvedPermissions(deniedPermissions, resolvedPermissions, role);

                // add permissions from parent roles
                var ancestorRoles = _roleService.GetRoleHierarchy(role, roles);
                foreach (var ancestorRole in ancestorRoles)
                {
                    resolvedPermissions = ancestorRole.Permissions
                        .Where(
                            p => IsActiveGrainAndSecurableItemMatch(p, grain, securableItem))
                        .Select(p => p.ToResolvedPermission(ResolvedPermission.Allow))
                        .ToList();

                    AddResolvedPermissions(allowedPermissions, resolvedPermissions, ancestorRole);

                    resolvedPermissions = ancestorRole.DeniedPermissions
                        .Select(p =>
                            p.ToResolvedPermission(ResolvedPermission.Deny))
                        .ToList();

                    AddResolvedPermissions(deniedPermissions, resolvedPermissions, ancestorRole);
                }
            }

            return new PermissionResolutionResult
            {
                AllowedPermissions = allowedPermissions.Distinct(),
                DeniedPermissions = deniedPermissions.Distinct()
            };
        }

        private static void AddResolvedPermissions(
            ICollection<ResolvedPermission> existingResolvedPermissions,
            IEnumerable<ResolvedPermission> newResolvedPermissions,
            Role role)
        {
            // attempt to add each newly resolved permission, filtering out duplicates
            foreach (var permission in newResolvedPermissions)
            {
                var existingResolvedPermission = existingResolvedPermissions.FirstOrDefault(p => p.Equals(permission));
                if (existingResolvedPermission == null)
                {
                    permission.Roles.Add(role.ToResolvedPermissionRole());
                    existingResolvedPermissions.Add(permission);
                }
                else
                {
                    // attempt to add the role to the list of inherited roles ensuring no duplicates are added
                    var existingResolvedPermissionRole =
                        existingResolvedPermission.Roles.FirstOrDefault(r => r.Id == role.Id);

                    if (existingResolvedPermissionRole == null)
                    {
                        existingResolvedPermission.Roles.Add(role.ToResolvedPermissionRole());
                    }
                }
            }
        }

        private static readonly Func<Permission, string, string, bool> IsActiveGrainAndSecurableItemMatch =
            (permission, grain, securableItem) =>
                !permission.IsDeleted &&
                (permission.Grain == grain || grain == null) &&
                (permission.SecurableItem == securableItem || securableItem == null);
    }
}