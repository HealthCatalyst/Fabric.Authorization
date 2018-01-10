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

        public SqlServerRoleStore(IAuthorizationDbContext authorizationDbContext)
        {
            _authorizationDbContext = authorizationDbContext;
        }

        public async Task<Role> Add(Role model)
        {
            //model.Id = Guid.NewGuid();
            var entity = model.ToEntity();

            _authorizationDbContext.Roles.Add(entity);
            await _authorizationDbContext.SaveChangesAsync();

            return entity.ToModel();
        }

        public async Task<Role> Get(Guid id)
        {
            var role = await GetEntityModel(id);
            return role.ToModel();
        }

        public async Task<EntityModels.Role> GetEntityModel(Guid id)
        {
            var role = await _authorizationDbContext.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .Include(r => r.GroupRoles)
                .ThenInclude(gr => gr.Group)
                .Include(r => r.SecurableItem)
                .SingleOrDefaultAsync(r =>
                    r.RoleId == id
                    && !r.IsDeleted);

            if (role == null)
            {
                throw new NotFoundException<Role>($"Could not find {typeof(Role).Name} entity with ID {id}");
            }

            return role;
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
            var roles = GetRoleEntityModels(grain, securableItem, roleName);
            return Task.FromResult(roles.Select(r => r.ToModel()).AsEnumerable());
        }

        private IEnumerable<EntityModels.Role> GetRoleEntityModels(string grain, string securableItem = null, string roleName = null)
        {
            var roles = _authorizationDbContext.Roles
                .Include(r => r.SecurableItem)
                .Where(r => !r.IsDeleted);

            if (!string.IsNullOrEmpty(grain))
            {
                roles = roles
                    .Where(r => string.Equals(r.Grain, grain, StringComparison.OrdinalIgnoreCase)
                                && !r.IsDeleted);
            }

            if (!string.IsNullOrWhiteSpace(securableItem))
            {
                roles = roles.Where(r => string.Equals(r.SecurableItem.Name, securableItem));
            }

            if (!string.IsNullOrWhiteSpace(roleName))
            {
                roles = roles.Where(r => string.Equals(r.Name, roleName));
            }

            return roles;
        }

        public async Task<Role> AddPermissionsToRole(Role role, ICollection<Permission> permissions)
        {
            // TODO: handle case where role.Id may not exist in Roles table
            foreach (var permission in permissions)
            {
                _authorizationDbContext.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permission.Id,
                    PermissionAction = PermissionAction.Allow
                });

                role.Permissions.Add(permission);
            }

            await _authorizationDbContext.SaveChangesAsync();
            return role;
        }

        public async Task<Role> RemovePermissionsFromRole(Role role, Guid[] permissionIds)
        {
            var roleEntity = await _authorizationDbContext.Roles
                .Include(r => r.RolePermissions)
                .SingleOrDefaultAsync(r =>
                    r.RoleId == role.Id
                    && !r.IsDeleted);

            if (roleEntity == null)
            {
                throw new NotFoundException<Role>($"Could not find {typeof(Role).Name} entity with ID {role.Id}");
            }

            foreach (var permissionId in permissionIds)
            {
                var rolePermissionToRemove = roleEntity.RolePermissions.Single(
                    rp => rp.RoleId == roleEntity.RoleId
                          && rp.PermissionId == permissionId);

                rolePermissionToRemove.IsDeleted = true;
                roleEntity.RolePermissions.Remove(rolePermissionToRemove);
            }

            await _authorizationDbContext.SaveChangesAsync();
            return roleEntity.ToModel();
        }

        public async Task RemovePermissionFromRoles(Guid permissionId, string grain, string securableItem = null)
        {
            var roles = GetRoleEntityModels(grain, securableItem);

            foreach (var role in roles)
            {
                var rolePermissionToRemove = role.RolePermissions.Single(
                    rp => rp.RoleId == role.RoleId
                          && rp.Permission.PermissionId == permissionId);

                if (rolePermissionToRemove != null)
                {
                    rolePermissionToRemove.IsDeleted = true;
                }
            }

            await _authorizationDbContext.SaveChangesAsync();
        }
    }
}