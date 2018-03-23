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
using Role = Fabric.Authorization.Domain.Models.Role;
using User = Fabric.Authorization.Domain.Models.User;

namespace Fabric.Authorization.Persistence.SqlServer.Stores
{
    public class SqlServerUserStore : IUserStore
    {
        private readonly IAuthorizationDbContext _authorizationDbContext;

        public SqlServerUserStore(IAuthorizationDbContext authorizationDbContext)
        {
            _authorizationDbContext = authorizationDbContext;
        }

        public async Task<User> Add(User model)
        {
            var entity = model.ToEntity();

            _authorizationDbContext.Users.Add(entity);
            await _authorizationDbContext.SaveChangesAsync();
            return entity.ToModel();
        }

        public async Task<User> Get(string id)
        {
            var idParts = SplitId(id);

            if (idParts.Length != 2)
            {
                throw new ArgumentException("id must be in the format {subjectId}:{identityProvider}");
            }

            var user = await _authorizationDbContext.Users
                .Include(u => u.GroupUsers)
                .ThenInclude(ug => ug.Group)
                .Include(u => u.UserPermissions)
                .ThenInclude(up => up.Permission)
                .Include(u => u.RoleUsers)
                .ThenInclude(u => u.Role)
                .ThenInclude(r => r.SecurableItem)
                .AsNoTracking()
                .SingleOrDefaultAsync(u =>
                    u.IdentityProvider.Equals(idParts[1], StringComparison.OrdinalIgnoreCase)
                    && u.SubjectId.Equals(idParts[0], StringComparison.OrdinalIgnoreCase)
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
            var users = await _authorizationDbContext.Users
                .Where(c => !c.IsDeleted)
                .ToArrayAsync();

            return users.Select(u => u.ToModel());
        }

        public async Task Delete(User model)
        {
            var user = await _authorizationDbContext.Users
                .SingleOrDefaultAsync(u =>
                    u.IdentityProvider.Equals(model.IdentityProvider, StringComparison.OrdinalIgnoreCase)
                    && u.SubjectId.Equals(model.SubjectId, StringComparison.OrdinalIgnoreCase)
                    && !u.IsDeleted);

            if (user == null)
            {
                throw new NotFoundException<User>($"Could not find {typeof(User).Name} User IDP = {model.IdentityProvider}, SubjectId = {model.SubjectId}");
            }

            user.IsDeleted = true;
            await _authorizationDbContext.SaveChangesAsync();
        }

        public async Task Update(User model)
        {
            var user = await _authorizationDbContext.Users
                .SingleOrDefaultAsync(u =>
                    u.IdentityProvider.Equals(model.IdentityProvider, StringComparison.OrdinalIgnoreCase)
                    && u.SubjectId.Equals(model.SubjectId, StringComparison.OrdinalIgnoreCase)
                    && !u.IsDeleted);

            if (user == null)
            {
                throw new NotFoundException<User>($"Could not find {typeof(User).Name} User IDP = {model.IdentityProvider}, SubjectId = {model.SubjectId}");
            }

            model.ToEntity(user);
            _authorizationDbContext.Users.Update(user);
            await _authorizationDbContext.SaveChangesAsync();
        }

        public async Task<bool> Exists(string id)
        {
            var idParts = SplitId(id);

            if (idParts.Length != 2)
            {
                throw new ArgumentException("id must be in the format {subjectId}:{identityProvider}");
            }

            var user = await _authorizationDbContext.Users
                .SingleOrDefaultAsync(u =>
                    u.IdentityProvider.Equals(idParts[1], StringComparison.OrdinalIgnoreCase)
                    && u.SubjectId.Equals(idParts[0], StringComparison.OrdinalIgnoreCase)
                    && !u.IsDeleted);

            return user != null;
        }

        public async Task<User> AddRolesToUser(User user, IList<Role> roles)
        {   
            foreach (var role in roles)
            {
                user.Roles.Add(role);
                _authorizationDbContext.RoleUsers.Add(new RoleUser
                {
                    IdentityProvider = user.IdentityProvider,
                    SubjectId = user.SubjectId,
                    RoleId = role.Id
                });
            }

            await _authorizationDbContext.SaveChangesAsync();
            return user;            
        }

        public async Task<User> DeleteRolesFromUser(User user, IList<Role> roles)
        {
            var roleUsers =
                _authorizationDbContext.RoleUsers.Where(
                    ru => ru.SubjectId.Equals(user.SubjectId, StringComparison.OrdinalIgnoreCase) &&
                          ru.IdentityProvider.Equals(user.IdentityProvider, StringComparison.OrdinalIgnoreCase) &&
                          !ru.IsDeleted &&
                          roles.Select(r => r.Id).Contains(ru.RoleId));

            foreach (var roleUser in roleUsers)
            {
                var roleToRemove = roles.FirstOrDefault(r => r.Id == roleUser.RoleId);
                if (roleToRemove != null)
                {
                    user.Roles.Remove(roleToRemove);
                    roleUser.IsDeleted = true;                    
                }
            }
            await _authorizationDbContext.SaveChangesAsync();
            return user;
        }
        
        private static string[] SplitId(string id)
        {
            var delimiter = new [] {@":"};
            return id.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}