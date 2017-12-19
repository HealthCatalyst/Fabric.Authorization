using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Persistence.SqlServer.Mappers;
using Fabric.Authorization.Persistence.SqlServer.Services;
using Microsoft.EntityFrameworkCore;

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
            var entity = model.ToEntity();

            _authorizationDbContext.Roles.Add(entity);
            await _authorizationDbContext.SaveChangesAsync();
            return model;
        }

        public async Task<Role> Get(Guid id)
        {
            var role = await _authorizationDbContext.Roles
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
                .Where(r => !r.IsDeleted)
                .ToArrayAsync();

            return roles.Select(p => p.ToModel());
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
                roles = roles.Where(p => string.Equals(p.SecurableItem.Name, securableItem));
            }

            if (!string.IsNullOrWhiteSpace(roleName))
            {
                roles = roles.Where(p => string.Equals(p.Name, roleName));
            }

            return Task.FromResult(roles.Select(p => p.ToModel()).AsEnumerable());
        }
    }
}