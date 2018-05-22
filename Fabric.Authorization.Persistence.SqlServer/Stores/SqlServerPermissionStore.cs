using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Events;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Persistence.SqlServer.EntityModels;
using Fabric.Authorization.Persistence.SqlServer.Mappers;
using Fabric.Authorization.Persistence.SqlServer.Services;
using Microsoft.EntityFrameworkCore;
using Permission = Fabric.Authorization.Domain.Models.Permission;
using User = Fabric.Authorization.Persistence.SqlServer.EntityModels.User;

namespace Fabric.Authorization.Persistence.SqlServer.Stores
{
    public class SqlServerPermissionStore : SqlServerBaseStore, IPermissionStore
    {
        public SqlServerPermissionStore(IAuthorizationDbContext authorizationDbContext, IEventService eventService) :
            base(authorizationDbContext, eventService)
        {
        }

        public async Task<Permission> Add(Permission permission)
        {
            permission.Id = Guid.NewGuid();
            var permissionEntity = permission.ToEntity();

            permissionEntity.SecurableItem =
                AuthorizationDbContext.SecurableItems.First(s => !s.IsDeleted && s.Name == permission.SecurableItem);

            AuthorizationDbContext.Permissions.Add(permissionEntity);

            await AuthorizationDbContext.SaveChangesAsync();
            await EventService.RaiseEventAsync(new EntityAuditEvent<Permission>(EventTypes.EntityCreatedEvent, permission.Id.ToString(), permissionEntity.ToModel()));
            return permissionEntity.ToModel();
        }

        public async Task<Permission> Get(Guid id)
        {
            var permissionEntity = await AuthorizationDbContext.Permissions
                .Include(p => p.SecurableItem)
                .SingleOrDefaultAsync(p =>
                    p.PermissionId == id
                    && !p.IsDeleted);

            if (permissionEntity == null)
            {
                throw new NotFoundException<Permission>(
                    $"Could not find {typeof(Permission).Name} entity with ID {id}");
            }

            return permissionEntity.ToModel();
        }

        public async Task<IEnumerable<Permission>> GetAll()
        {
            var permissions = await AuthorizationDbContext.Permissions
                .Where(p => !p.IsDeleted)
                .ToArrayAsync();

            return permissions.Select(p => p.ToModel());
        }

        public async Task Delete(Permission permission)
        {
            var permissionEntity = await AuthorizationDbContext.Permissions
                .Include(p => p.RolePermissions)
                .Include(p => p.UserPermissions)
                .SingleOrDefaultAsync(p =>
                    p.PermissionId == permission.Id
                    && !p.IsDeleted);

            if (permissionEntity == null)
            {
                throw new NotFoundException<Permission>(
                    $"Could not find {typeof(Permission).Name} entity with ID {permission.Id}");
            }

            permissionEntity.IsDeleted = true;

            foreach (var rolePermission in permissionEntity.RolePermissions)
            {
                rolePermission.IsDeleted = true;
            }

            foreach (var userPermission in permissionEntity.UserPermissions)
            {
                userPermission.IsDeleted = true;
            }

            await AuthorizationDbContext.SaveChangesAsync();
            await EventService.RaiseEventAsync(new EntityAuditEvent<Permission>(EventTypes.EntityDeletedEvent, permission.Id.ToString(), permissionEntity.ToModel()));
        }

        public async Task Update(Permission permission)
        {
            var permissionEntity = await AuthorizationDbContext.Permissions
                .SingleOrDefaultAsync(p =>
                    p.PermissionId == permission.Id
                    && !p.IsDeleted);

            if (permissionEntity == null)
            {
                throw new NotFoundException<Permission>(
                    $"Could not find {typeof(Permission).Name} entity with ID {permission.Id}");
            }

            permission.ToEntity(permissionEntity);
            AuthorizationDbContext.Permissions.Update(permissionEntity);
            await AuthorizationDbContext.SaveChangesAsync();
            await EventService.RaiseEventAsync(new EntityAuditEvent<Permission>(EventTypes.EntityUpdatedEvent, permission.Id.ToString(), permissionEntity.ToModel()));
        }

        public async Task<bool> Exists(Guid id)
        {
            var permission = await AuthorizationDbContext.Permissions
                .SingleOrDefaultAsync(p =>
                    p.PermissionId == id
                    && !p.IsDeleted);

            return permission != null;
        }

        public Task<IEnumerable<Permission>> GetPermissions(string grain, string securableItem = null,
            string permissionName = null)
        {
            var permissions = AuthorizationDbContext.Permissions
                .Include(p => p.SecurableItem)
                .Where(p => p.Grain == grain
                            && !p.IsDeleted);

            if (!string.IsNullOrWhiteSpace(securableItem))
            {
                permissions = permissions.Where(p => p.SecurableItem.Name == securableItem);
            }

            if (!string.IsNullOrWhiteSpace(permissionName))
            {
                permissions = permissions.Where(p => p.Name == permissionName);
            }

            return Task.FromResult(permissions.Select(p => p.ToModel()).AsEnumerable());
        }

        public async Task AddOrUpdateGranularPermission(GranularPermission granularPermission)
        {
            var idParts = SplitGranularPermissionId(granularPermission.Id);

            var subjectId = idParts[0];
            var identityProvider = idParts[1];

            var userEntity = await AuthorizationDbContext.Users
                .Include(u => u.UserPermissions)
                .ThenInclude(up => up.Permission)
                .SingleOrDefaultAsync(u => u.IdentityProvider == identityProvider
                                           && u.SubjectId == subjectId
                                           && !u.IsDeleted);

            var userCreated = false;
            if (userEntity == null)
            {
                userEntity = new User
                {
                    IdentityProvider = identityProvider,
                    SubjectId = subjectId,
                    Name = $"{identityProvider}\\{subjectId}"
                };
                AuthorizationDbContext.Users.Add(userEntity);
                userCreated = true;
            }

            // remove all current permissions first and then replace them with the new set of permissions
            var currentUserPermissions = userEntity.UserPermissions.Where(up => !up.IsDeleted);
            foreach (var userPermission in currentUserPermissions)
            {
                userPermission.IsDeleted = true;
            }

            await AuthorizationDbContext.UserPermissions.AddRangeAsync(granularPermission.AdditionalPermissions.Select(
                ap => new UserPermission
                {
                    SubjectId = userEntity.SubjectId,
                    IdentityProvider = userEntity.IdentityProvider,
                    PermissionId = ap.Id,
                    PermissionAction = PermissionAction.Allow
                }));

            await AuthorizationDbContext.UserPermissions.AddRangeAsync(granularPermission.DeniedPermissions.Select(
                dp => new UserPermission
                {
                    SubjectId = userEntity.SubjectId,
                    IdentityProvider = userEntity.IdentityProvider,
                    PermissionId = dp.Id,
                    PermissionAction = PermissionAction.Deny
                }));

            await AuthorizationDbContext.SaveChangesAsync();

            if (userCreated)
            {
                await EventService.RaiseEventAsync(new EntityAuditEvent<Domain.Models.User>(EventTypes.EntityCreatedEvent, granularPermission.Id, userEntity.ToModel()));
            }

            await EventService.RaiseEventAsync(new EntityAuditEvent<GranularPermission>(EventTypes.EntityUpdatedEvent, granularPermission.Id, granularPermission));
        }

        public async Task<GranularPermission> GetGranularPermission(string userId)
        {
            var idParts = SplitGranularPermissionId(userId);

            var subjectId = idParts[0];
            var identityProvider = idParts[1];

            var user = await AuthorizationDbContext.Users
                .Include(u => u.UserPermissions)
                .ThenInclude(up => up.Permission)
                .ThenInclude(p => p.SecurableItem)
                .SingleOrDefaultAsync(u => u.IdentityProvider == identityProvider
                                           && u.SubjectId == subjectId
                                           && !u.IsDeleted);

            if (user == null)
            {
                return new GranularPermission();
            }

            var userPermissions = user.UserPermissions.Where(up => !up.IsDeleted).ToList();
            var allowedUserPermissions = userPermissions.Where(up => up.PermissionAction == PermissionAction.Allow);
            var deniedUserPermissions = userPermissions.Where(up => up.PermissionAction == PermissionAction.Deny);

            return new GranularPermission
            {
                AdditionalPermissions = allowedUserPermissions.Select(aup => aup.Permission.ToModel()),
                DeniedPermissions = deniedUserPermissions.Select(dup => dup.Permission.ToModel())
            };
        }

        private static string[] SplitGranularPermissionId(string id)
        {
            var delimiter = new[] {@":"};
            var idParts = id.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
            if (idParts.Length != 2)
            {
                throw new ArgumentException(
                    "The granular permission id was not in the format {subjectId}:{identityProvider}");
            }

            return idParts;
        }
    }
}