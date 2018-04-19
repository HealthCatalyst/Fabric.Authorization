using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Persistence.SqlServer.EntityModels;
using Fabric.Authorization.Persistence.SqlServer.Mappers;
using Fabric.Authorization.Persistence.SqlServer.Services;
using Microsoft.EntityFrameworkCore;
using Role = Fabric.Authorization.Domain.Models.Role;
using User = Fabric.Authorization.Domain.Models.User;

namespace Fabric.Authorization.Persistence.SqlServer.Stores
{
    public class SqlServerUserStore : SqlServerBaseStore, IUserStore
    {
        public SqlServerUserStore(IAuthorizationDbContext authorizationDbContext, IEventService eventService) : 
            base(authorizationDbContext, eventService)
        {
        }

        public async Task<User> Add(User model)
        {
            var entity = model.ToEntity();

            AuthorizationDbContext.Users.Add(entity);
            await AuthorizationDbContext.SaveChangesAsync();
            return entity.ToModel();
        }

        public async Task<User> Get(string id)
        {
            var idParts = SplitId(id);

            if (idParts.Length != 2)
            {
                throw new ArgumentException("id must be in the format {subjectId}:{identityProvider}");
            }

            var subjectId = idParts[0];
            var identityProvider = idParts[1];

            var user = await AuthorizationDbContext.Users
                .Include(u => u.GroupUsers)
                .ThenInclude(ug => ug.Group)
                .Include(u => u.UserPermissions)
                .ThenInclude(up => up.Permission)
                .Include(u => u.RoleUsers)
                .ThenInclude(u => u.Role)
                .ThenInclude(r => r.SecurableItem)
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.IdentityProvider == identityProvider
                                           && u.SubjectId == subjectId
                                           && !u.IsDeleted);

            if (user == null)
            {
                throw new NotFoundException<User>($"Could not find {typeof(User).Name} entity with ID {id}");
            }

            user.GroupUsers = user.GroupUsers.Where(gu => !gu.IsDeleted).ToList();
            user.UserPermissions = user.UserPermissions.Where(up => !up.IsDeleted).ToList();
            user.RoleUsers = user.RoleUsers.Where(ru => !ru.IsDeleted).ToList();

            return user.ToModel();
        }

        public async Task<IEnumerable<User>> GetAll()
        {
            var users = await AuthorizationDbContext.Users
                .Where(c => !c.IsDeleted)
                .ToArrayAsync();

            return users.Select(u => u.ToModel());
        }

        public async Task Delete(User model)
        {
            var user = await AuthorizationDbContext.Users
                .SingleOrDefaultAsync(u =>
                    u.IdentityProvider == model.IdentityProvider
                    && u.SubjectId == model.SubjectId
                    && !u.IsDeleted);

            if (user == null)
            {
                throw new NotFoundException<User>(
                    $"Could not find {typeof(User).Name} User IDP = {model.IdentityProvider}, SubjectId = {model.SubjectId}");
            }

            user.IsDeleted = true;
            await AuthorizationDbContext.SaveChangesAsync();
        }

        public async Task Update(User model)
        {
            var user = await AuthorizationDbContext.Users
                .SingleOrDefaultAsync(u =>
                    u.IdentityProvider == model.IdentityProvider
                    && u.SubjectId == model.SubjectId
                    && !u.IsDeleted);

            if (user == null)
            {
                throw new NotFoundException<User>(
                    $"Could not find {typeof(User).Name} User IDP = {model.IdentityProvider}, SubjectId = {model.SubjectId}");
            }

            model.ToEntity(user);
            AuthorizationDbContext.Users.Update(user);
            await AuthorizationDbContext.SaveChangesAsync();
        }

        public async Task<bool> Exists(string id)
        {
            var idParts = SplitId(id);

            if (idParts.Length != 2)
            {
                throw new ArgumentException("id must be in the format {subjectId}:{identityProvider}");
            }

            var subjectId = idParts[0];
            var identityProvider = idParts[1];

            var user = await AuthorizationDbContext.Users
                .SingleOrDefaultAsync(u =>
                    u.IdentityProvider == identityProvider
                    && u.SubjectId == subjectId
                    && !u.IsDeleted);

            return user != null;
        }

        public async Task<User> AddRolesToUser(User user, IList<Role> roles)
        {
            foreach (var role in roles)
            {
                user.Roles.Add(role);
                AuthorizationDbContext.RoleUsers.Add(new RoleUser
                {
                    IdentityProvider = user.IdentityProvider,
                    SubjectId = user.SubjectId,
                    RoleId = role.Id
                });
            }

            await AuthorizationDbContext.SaveChangesAsync();
            return user;
        }

        public async Task<User> DeleteRolesFromUser(User user, IList<Role> roles)
        {
            var roleIds = roles.Select(r => r.Id);

            var roleUsers =
                AuthorizationDbContext.RoleUsers.Where(
                    ru => ru.SubjectId == user.SubjectId &&
                          ru.IdentityProvider == user.IdentityProvider &&
                          !ru.IsDeleted &&
                          roleIds.Contains(ru.RoleId));

            foreach (var roleUser in roleUsers)
            {
                var roleToRemove = roles.FirstOrDefault(r => r.Id == roleUser.RoleId);
                if (roleToRemove != null)
                {
                    user.Roles.Remove(roleToRemove);
                    roleUser.IsDeleted = true;
                }
            }

            await AuthorizationDbContext.SaveChangesAsync();
            return user;
        }

        private static string[] SplitId(string id)
        {
            var delimiter = new[] {@":"};
            return id.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}