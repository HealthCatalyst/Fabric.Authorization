using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores
{
    public interface IRoleStore : IGenericStore<Guid, Role>
    {
        Task<IEnumerable<Role>> GetRoles(string grain, string securableItem = null, string roleName = null);
        Task<Role> AddPermissionsToRole(Role role, ICollection<Permission> allowPermissions, ICollection<Permission> denyPermissions);
        Task<Role> RemovePermissionsFromRole(Role role, Guid[] permissionIds);
        Task RemovePermissionFromRoles(Guid permissionId, string grain, string securableItem = null);
    }
}