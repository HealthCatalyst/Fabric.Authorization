using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Events;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Persistence.SqlServer.EntityModels;
using Fabric.Authorization.Persistence.SqlServer.Mappers;
using Fabric.Authorization.Persistence.SqlServer.Services;
using Microsoft.EntityFrameworkCore;
using Permission = Fabric.Authorization.Domain.Models.Permission;
using Role = Fabric.Authorization.Domain.Models.Role;

namespace Fabric.Authorization.Persistence.SqlServer.Stores
{
    public class SqlServerRoleStore : SqlServerBaseStore, IRoleStore
    {
        public SqlServerRoleStore(IAuthorizationDbContext authorizationDbContext, IEventService eventService) :
            base(authorizationDbContext, eventService)
        {
        }

        public async Task<Role> Add(Role role)
        {
            role.Id = Guid.NewGuid();
            var roleEntity = role.ToEntity();
            roleEntity.SecurableItem =
                AuthorizationDbContext.SecurableItems.First(s => !s.IsDeleted && s.Name == role.SecurableItem);

            if (role.ChildRoles.Any())
            {
                var childRoles = await AuthorizationDbContext.Roles.Where(r => role.ChildRoles.Contains(r.RoleId))
                    .ToListAsync();
                foreach (var childRole in childRoles)
                {
                    childRole.ParentRole = roleEntity;
                }
            }

            AuthorizationDbContext.Roles.Add(roleEntity);
            await AuthorizationDbContext.SaveChangesAsync();
            await EventService.RaiseEventAsync(new EntityAuditEvent<Role>(EventTypes.EntityCreatedEvent, role.Id.ToString(), roleEntity.ToModel()));
            return roleEntity.ToModel();
        }

        public async Task<Role> Get(Guid id)
        {
            var role = await GetEntityModel(id);
            return role.ToModel();
        }

        public async Task<IEnumerable<Role>> GetAll()
        {
            var roles = await AuthorizationDbContext.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .Include(r => r.GroupRoles)
                .ThenInclude(gr => gr.Group)
                .Where(r => !r.IsDeleted)
                .ToArrayAsync();

            return roles.Select(r => r.ToModel());
        }

        public async Task Delete(Role role)
        {
            var roleEntity = await AuthorizationDbContext.Roles
                .Include(r => r.RolePermissions)
                .Include(r => r.GroupRoles)
                .SingleOrDefaultAsync(r =>
                    r.RoleId == role.Id
                    && !r.IsDeleted);

            if (roleEntity == null)
            {
                throw new NotFoundException<Role>($"Could not find {typeof(Role).Name} entity with ID {role.Id}");
            }

            roleEntity.IsDeleted = true;

            foreach (var rolePermission in roleEntity.RolePermissions)
            {
                rolePermission.IsDeleted = true;
            }

            foreach (var groupRole in roleEntity.GroupRoles)
            {
                groupRole.IsDeleted = true;
            }

            await AuthorizationDbContext.SaveChangesAsync();
            await EventService.RaiseEventAsync(new EntityAuditEvent<Role>(EventTypes.EntityDeletedEvent, role.Id.ToString(), roleEntity.ToModel()));
        }

        public async Task Update(Role role)
        {
            var roleEntity = await AuthorizationDbContext.Roles
                .SingleOrDefaultAsync(r =>
                    r.RoleId == role.Id
                    && !r.IsDeleted);

            if (roleEntity == null)
            {
                throw new NotFoundException<Role>($"Could not find {typeof(Role).Name} entity with ID {role.Id}");
            }

            role.ToEntity(roleEntity);
            AuthorizationDbContext.Roles.Update(roleEntity);
            await AuthorizationDbContext.SaveChangesAsync();
            await EventService.RaiseEventAsync(new EntityAuditEvent<Role>(EventTypes.EntityUpdatedEvent, role.Id.ToString(), roleEntity.ToModel()));
        }

        public async Task<bool> Exists(Guid id)
        {
            var role = await AuthorizationDbContext.Roles
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

        public async Task<Role> AddPermissionsToRole(Role role, ICollection<Permission> allowPermissions,
            ICollection<Permission> denyPermissions)
        {
            // TODO: handle case where role.Id may not exist in Roles table
            foreach (var permission in allowPermissions)
            {
                AuthorizationDbContext.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permission.Id,
                    PermissionAction = PermissionAction.Allow
                });

                role.Permissions.Add(permission);
            }

            foreach (var permission in denyPermissions)
            {
                AuthorizationDbContext.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permission.Id,
                    PermissionAction = PermissionAction.Deny
                });

                role.Permissions.Add(permission);
            }

            await AuthorizationDbContext.SaveChangesAsync();
            await EventService.RaiseEventAsync(new EntityAuditEvent<Role>(EventTypes.EntityUpdatedEvent, role.Id.ToString(), role));
            return role;
        }

        public async Task<Role> RemovePermissionsFromRole(Role role, Guid[] permissionIds)
        {
            var roleEntity = await AuthorizationDbContext.Roles
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
                AuthorizationDbContext.RolePermissions.Update(rolePermissionToRemove);
            }

            await AuthorizationDbContext.SaveChangesAsync();
            await EventService.RaiseEventAsync(new EntityAuditEvent<Role>(EventTypes.EntityUpdatedEvent, role.Id.ToString(), role));
            return roleEntity.ToModel();
        }

        public async Task RemovePermissionFromRoles(Guid permissionId, string grain, string securableItem = null)
        {
            var roles = GetRoleEntityModels(grain, securableItem).ToList();

            foreach (var role in roles)
            {
                var rolePermissionToRemove = role.RolePermissions.SingleOrDefault(
                    rp => rp.RoleId == role.RoleId
                          && rp.Permission.PermissionId == permissionId);

                if (rolePermissionToRemove != null)
                {
                    rolePermissionToRemove.IsDeleted = true;
                    AuthorizationDbContext.RolePermissions.Update(rolePermissionToRemove);
                }
            }

            await AuthorizationDbContext.SaveChangesAsync();
            await EventService.RaiseEventAsync(new EntityBatchAuditEvent<EntityModels.Role>(EventTypes.EntityUpdatedEvent, roles));
        }

        private async Task<EntityModels.Role> GetEntityModel(Guid id)
        {
            var roleEntity = await AuthorizationDbContext.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .Include(r => r.GroupRoles)
                .ThenInclude(gr => gr.Group)
                .Include(r => r.SecurableItem)
                .SingleOrDefaultAsync(r =>
                    r.RoleId == id
                    && !r.IsDeleted);

            if (roleEntity == null)
            {
                throw new NotFoundException<Role>($"Could not find {typeof(Role).Name} entity with ID {id}");
            }

            return roleEntity;
        }

        private IEnumerable<EntityModels.Role> GetRoleEntityModels(string grain, string securableItem = null,
            string roleName = null)
        {
            var roles = AuthorizationDbContext.Roles
                .Include(r => r.SecurableItem)
                .Include(r => r.ParentRole)
                .Include(r => r.ChildRoles)
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .ThenInclude(p => p.SecurableItem)
                .Include(r => r.GroupRoles)
                .ThenInclude(gr => gr.Group)
                .ThenInclude(g => g.GroupRoles)
                .ThenInclude(gr => gr.Role)
                .ThenInclude(r => r.SecurableItem)
                .Include(r => r.RoleUsers)
                .ThenInclude(ru => ru.User)
                .ThenInclude(u => u.RoleUsers)
                .ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.SecurableItem)
                .Include(r => r.RoleUsers)
                .ThenInclude(ru => ru.User)
                .ThenInclude(u => u.GroupUsers)
                .ThenInclude(gu => gu.Group)
                .AsNoTracking()
                .Where(r => !r.IsDeleted);

            if (!string.IsNullOrEmpty(grain))
            {
                roles = roles.Where(r => r.Grain == grain);
            }

            if (!string.IsNullOrWhiteSpace(securableItem))
            {
                roles = roles.Where(r => r.SecurableItem.Name == securableItem);
            }

            if (!string.IsNullOrWhiteSpace(roleName))
            {
                roles = roles.Where(r => r.Name == roleName);
            }

            foreach (var role in roles)
            {
                role.RoleUsers = role.RoleUsers.Where(ru => !ru.IsDeleted).ToList();
                role.GroupRoles = role.GroupRoles.Where(gr => !gr.IsDeleted).ToList();
                role.RolePermissions = role.RolePermissions.Where(rp => !rp.IsDeleted).ToList();
            }

            return roles;
        }
    }
}