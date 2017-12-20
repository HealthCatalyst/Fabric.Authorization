using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Persistence.SqlServer.EntityModels;
using Fabric.Authorization.Persistence.SqlServer.Mappers;
using Fabric.Authorization.Persistence.SqlServer.Services;
using Microsoft.EntityFrameworkCore;
using Permission = Fabric.Authorization.Domain.Models.Permission;
using Role = Fabric.Authorization.Domain.Models.Role;

namespace Fabric.Authorization.Persistence.SqlServer.Stores
{
    public class SqlServerRoleStore : IRoleStore
    {
        private readonly IAuthorizationDbContext _authorizationDbContext;
        private readonly IPermissionStore _permissionStore;

        public SqlServerRoleStore(IAuthorizationDbContext authorizationDbContext, IPermissionStore permissionStore)
        {
            _authorizationDbContext = authorizationDbContext;
            _permissionStore = permissionStore ?? throw new ArgumentNullException(nameof(permissionStore));
        }

        public async Task<Role> Add(Role model)
        {
            var entity = model.ToEntity();

            _authorizationDbContext.Roles.Add(entity);
            await _authorizationDbContext.SaveChangesAsync();
            return model;
        }

        public async Task<Role> Get(Guid id)
        {
            var role = await _authorizationDbContext.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .Include(r => r.GroupRoles)
                .ThenInclude(gr => gr.Group)
                .SingleOrDefaultAsync(r =>
                    r.RoleId == id
                    && !r.IsDeleted);

            if (role == null)
            {
                throw new NotFoundException<Role>($"Could not find {typeof(Role).Name} entity with ID {id}");
            }

            return role.ToModel();
        }

        public async Task<IEnumerable<Role>> GetAll()
        {
            var roles = await _authorizationDbContext.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .Include(r => r.GroupRoles)
                .ThenInclude(gr => gr.Group)
                .Where(r => !r.IsDeleted)
                .ToArrayAsync();

            return roles.Select(r => r.ToModel());
        }

        public async Task Delete(Role model)
        {
            var role = await _authorizationDbContext.Roles
                .Include(r => r.RolePermissions)
                .Include(r => r.GroupRoles)
                .SingleOrDefaultAsync(r =>
                    r.RoleId == model.Id
                    && !r.IsDeleted);

            if (role == null)
            {
                throw new NotFoundException<Role>($"Could not find {typeof(Role).Name} entity with ID {model.Id}");
            }

            role.IsDeleted = true;

            foreach (var rolePermission in role.RolePermissions)
            {
                rolePermission.IsDeleted = true;
            }

            foreach (var groupRole in role.GroupRoles)
            {
                groupRole.IsDeleted = true;
            }

            await _authorizationDbContext.SaveChangesAsync();
        }

        public async Task Update(Role model)
        {
            var role = await _authorizationDbContext.Roles
                .SingleOrDefaultAsync(r =>
                    r.RoleId == model.Id
                    && !r.IsDeleted);

            if (role == null)
            {
                throw new NotFoundException<Role>($"Could not find {typeof(Role).Name} entity with ID {model.Id}");
            }

            model.ToEntity(role);
            _authorizationDbContext.Roles.Update(role);
            await _authorizationDbContext.SaveChangesAsync();
        }

        public async Task<bool> Exists(Guid id)
        {
            var role = await _authorizationDbContext.Roles
                .SingleOrDefaultAsync(r =>
                    r.RoleId == id
                    && !r.IsDeleted);

            return role != null;
        }

        public Task<IEnumerable<Role>> GetRoles(string grain, string securableItem = null, string roleName = null)
        {
            var roles = _authorizationDbContext.Roles
                .Include(r => r.SecurableItem)
                .Where(r => string.Equals(r.Grain, grain, StringComparison.OrdinalIgnoreCase)
                            && !r.IsDeleted);

            if (!string.IsNullOrWhiteSpace(securableItem))
            {
                roles = roles.Where(r => string.Equals(r.SecurableItem.Name, securableItem));
            }

            if (!string.IsNullOrWhiteSpace(roleName))
            {
                roles = roles.Where(r => string.Equals(r.Name, roleName));
            }

            return Task.FromResult(roles.Select(r => r.ToModel()).AsEnumerable());
        }

        public async Task<Role> AddPermissionsToRole(Role role, ICollection<Permission> permissions)
        {
            var roleEntity = await _authorizationDbContext.Roles
                .SingleOrDefaultAsync(r =>
                    r.RoleId == role.Id
                    && !r.IsDeleted);

            if (role == null)
            {
                throw new NotFoundException<Role>($"Could not find {typeof(Role).Name} entity with ID {role.Id}");
            }

            var permissionIds = permissions.Select(p => p.Id);
            var permissionDictionary = _authorizationDbContext.Permissions
                .Where(p => !p.IsDeleted)
                .Where(p => permissionIds.Contains(p.PermissionId))
                .ToDictionary(p => p.PermissionId);

            foreach (var permission in permissions)
            {
                _authorizationDbContext.RolePermissions.Add(new RolePermission
                {
                    RoleId = roleEntity.Id,
                    PermissionId = permissionDictionary[permission.Id].Id,
                    PermissionAction = PermissionAction.Allow
                });
            }

            await _authorizationDbContext.SaveChangesAsync();

            var updatedRole = await Get(role.Id);
            return updatedRole;
        }

        public async Task<Role> RemovePermissionsFromRole(Role role, Guid[] permissionIds)
        {
            foreach (var permissionId in permissionIds)
            {
                if (role.Permissions.All(p => p.Id != permissionId))
                {
                    throw new NotFoundException<Permission>($"Permission with id {permissionId} not found on role {role.Id}");
                }
            }

            foreach (var permissionId in permissionIds)
            {
                var permission = role.Permissions.First(p => p.Id == permissionId);
                role.Permissions.Remove(permission);
            }

            await Update(role);
            return role;
        }

        public async Task RemovePermissionsFromRoles(Guid permissionId, string grain, string securableItem = null)
        {
            var roles = await GetRoles(grain, securableItem);

            // TODO: candidate for batch update
            foreach (var role in roles)
            {
                if (role.Permissions != null && role.Permissions.Any(p => p.Id == permissionId))
                {
                    var permission = role.Permissions.First(p => p.Id == permissionId);
                    role.Permissions.Remove(permission);
                    await Update(role);
                }
            }
        }
    }
}