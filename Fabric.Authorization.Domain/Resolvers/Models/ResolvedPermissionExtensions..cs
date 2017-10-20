using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Resolvers.Models
{
    public static class ResolvedPermissionExtensions
    {
        public static ResolvedPermission ToResolvedPermission(this Permission permission, string action,
            Role role = null)
        {
            return new ResolvedPermission
            {
                Id = permission.Id,
                Grain = permission.Grain,
                SecurableItem = permission.SecurableItem,
                Name = permission.Name,
                Action = action,
                CreatedDateTimeUtc = permission.CreatedDateTimeUtc,
                ModifiedDateTimeUtc = permission.ModifiedDateTimeUtc,
                CreatedBy = permission.CreatedBy,
                ModifiedBy = permission.ModifiedBy
            };
        }

        public static ResolvedPermissionRole ToResolvedPermissionRole(this Role role)
        {
            return new ResolvedPermissionRole
            {
                Id = role.Id,
                Name = role.Name
            };
        }
    }
}