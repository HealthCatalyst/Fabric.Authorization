using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Persistence.SqlServer.EntityModels;
using Fabric.Authorization.Persistence.SqlServer.Mappers;
using Fabric.Authorization.Persistence.SqlServer.Services;
using Microsoft.EntityFrameworkCore;
using Permission = Fabric.Authorization.Domain.Models.Permission;
using User = Fabric.Authorization.Domain.Models.User;

namespace Fabric.Authorization.Persistence.SqlServer.Stores
{
    public class SqlServerPermissionStore : IPermissionStore
    {
        private readonly IAuthorizationDbContext _authorizationDbContext;

        public SqlServerPermissionStore(IAuthorizationDbContext authorizationDbContext)
        {
            _authorizationDbContext = authorizationDbContext;
        }

        public async Task<Permission> Add(Permission model)
        {
            var entity = model.ToEntity();

            _authorizationDbContext.Permissions.Add(entity);
            await _authorizationDbContext.SaveChangesAsync();
            return model;
        }

        public async Task<Permission> Get(Guid id)
        {
            var permission = await _authorizationDbContext.Permissions
                .SingleOrDefaultAsync(p =>
                    p.PermissionId == id
                    && !p.IsDeleted);

            if (permission == null)
            {
                throw new NotFoundException<Permission>($"Could not find {typeof(Permission).Name} entity with ID {id}");
            }

            return permission.ToModel();
        }

        public async Task<IEnumerable<Permission>> GetAll()
        {
            var permissions = await _authorizationDbContext.Permissions
                .Where(p => !p.IsDeleted)
                .ToArrayAsync();

            return permissions.Select(p => p.ToModel());
        }

        public async Task Delete(Permission model)
        {
            var permission = await _authorizationDbContext.Permissions
                .Include(p => p.RolePermissions)
                .Include(p => p.UserPermissions)
                .SingleOrDefaultAsync(p =>
                    p.PermissionId == model.Id
                    && !p.IsDeleted);

            if (permission == null)
            {
                throw new NotFoundException<User>($"Could not find {typeof(Permission).Name} entity with ID {model.Id}");
            }

            permission.IsDeleted = true;

            foreach (var rolePermission in permission.RolePermissions)
            {
                rolePermission.IsDeleted = true;
            }

            foreach (var userPermission in permission.UserPermissions)
            {
                userPermission.IsDeleted = true;
            }

            await _authorizationDbContext.SaveChangesAsync();
        }

        public async Task Update(Permission model)
        {
            var permission = await _authorizationDbContext.Permissions
                .SingleOrDefaultAsync(p =>
                    p.PermissionId == model.Id
                    && !p.IsDeleted);

            if (permission == null)
            {
                throw new NotFoundException<User>($"Could not find {typeof(Permission).Name} entity with ID {model.Id}");
            }

            model.ToEntity(permission);
            _authorizationDbContext.Permissions.Update(permission);
            await _authorizationDbContext.SaveChangesAsync();
        }

        public async Task<bool> Exists(Guid id)
        {
            var permission = await _authorizationDbContext.Permissions
                .SingleOrDefaultAsync(p =>
                    p.PermissionId == id
                    && !p.IsDeleted);

            return permission != null;
        }

        public Task<IEnumerable<Permission>> GetPermissions(string grain, string securableItem = null,
            string permissionName = null)
        {
            var permissions = _authorizationDbContext.Permissions
                .Include(p => p.SecurableItem)
                .Where(p => string.Equals(p.Grain, grain, StringComparison.OrdinalIgnoreCase)
                            && !p.IsDeleted);

            if (!string.IsNullOrWhiteSpace(securableItem))
            {
                permissions = permissions.Where(p => string.Equals(p.SecurableItem.Name, securableItem));
            }

            if (!string.IsNullOrWhiteSpace(permissionName))
            {
                permissions = permissions.Where(p => string.Equals(p.Name, permissionName));
            }

            return Task.FromResult(permissions.Select(p => p.ToModel()).AsEnumerable());
        }

        public async Task AddOrUpdateGranularPermission(GranularPermission granularPermission)
        {
            var idParts = SplitUserId(granularPermission.Id);

            var user = await _authorizationDbContext.Users
                .Include(u => u.UserPermissions)
                .ThenInclude(up => up.Permission)
                .SingleOrDefaultAsync(u => u.IdentityProvider.Equals(idParts[0], StringComparison.OrdinalIgnoreCase)
                                           && u.SubjectId.Equals(idParts.Length > 1 ? idParts[1] : idParts[0], StringComparison.OrdinalIgnoreCase)
                                           && !u.IsDeleted);

            if (user == null)
            {
                throw new NotFoundException<User>($"Could not find {typeof(User).Name} User ID {granularPermission.Id}");
            }

            // remove all current permissions first and then replace them with the new set of permissions
            var currentUserPermissions = user.UserPermissions.Where(up => !up.IsDeleted);
            foreach (var userPermission in currentUserPermissions)
            {
                _authorizationDbContext.UserPermissions.Remove(userPermission);
            }

            var additionalPermissionIds = granularPermission.AdditionalPermissions.Select(gp => gp.Id);
            var deniedPermissionIds = granularPermission.DeniedPermissions.Select(gp => gp.Id);

            // retrieve all permissions that a passed in and store in memory for Id lookups below
            var permissionDictionary = _authorizationDbContext.Permissions.Where(p => !p.IsDeleted)
                .Where(p => additionalPermissionIds.Contains(p.PermissionId) || deniedPermissionIds.Contains(p.PermissionId))
                .ToDictionary(p => p.PermissionId);

            await _authorizationDbContext.UserPermissions.AddRangeAsync(granularPermission.AdditionalPermissions.Select(
                ap => new UserPermission
                {
                    UserId = user.Id,
                    PermissionId = permissionDictionary[ap.Id].Id,
                    PermissionAction = PermissionAction.Allow
                }));

            await _authorizationDbContext.UserPermissions.AddRangeAsync(granularPermission.DeniedPermissions.Select(
                dp => new UserPermission
                {
                    UserId = user.Id,
                    PermissionId = permissionDictionary[dp.Id].Id,
                    PermissionAction = PermissionAction.Deny
                }));

            await _authorizationDbContext.SaveChangesAsync();
        }

        public async Task<GranularPermission> GetGranularPermission(string userId)
        {
            var idParts = SplitUserId(userId);

            var user = await _authorizationDbContext.Users
                .Include(u => u.UserPermissions)
                .ThenInclude(up => up.Permission)
                .SingleOrDefaultAsync(u => u.IdentityProvider.Equals(idParts[0], StringComparison.OrdinalIgnoreCase)
                            && u.SubjectId.Equals(idParts.Length > 1 ? idParts[1] : idParts[0], StringComparison.OrdinalIgnoreCase)
                            && !u.IsDeleted);

            if (user == null)
            {
                throw new NotFoundException<User>($"Could not find {typeof(User).Name} entity with ID {userId}");
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

        /// <summary>
        /// TODO: consolidate this method and same method in SqlServerUser store
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private static string[] SplitUserId(string id)
        {
            var delimiter = new[] { @"\" };
            return id.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}