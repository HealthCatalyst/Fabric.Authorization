using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Persistence.SqlServer.Mappers;
using Fabric.Authorization.Persistence.SqlServer.Services;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Authorization.Persistence.SqlServer.Stores
{
    public class SqlServerGroupStore : IGroupStore
    {
        private readonly IAuthorizationDbContext _authorizationDbContext;

        public SqlServerGroupStore(IAuthorizationDbContext authorizationDbContext)
        {
            _authorizationDbContext = authorizationDbContext;
        }

        public async Task<Group> Add(Group model)
        {
            var groupEntity = model.ToEntity();

            _authorizationDbContext.Groups.Add(groupEntity);
            await _authorizationDbContext.SaveChangesAsync();

            return model;
        }

        public async Task<Group> Get(string id)
        {
            var group = await _authorizationDbContext.Groups
                .Include(g => g.GroupRoles)
                .ThenInclude(gr => gr.Role)
                .Include(g => g.GroupUsers)
                .ThenInclude(gu => gu.User)
                .SingleOrDefaultAsync(g => g.GroupId.Equals(id, StringComparison.OrdinalIgnoreCase)
                && !g.IsDeleted);

            if (group == null)
            {
                throw new NotFoundException<Group>($"Could not find {typeof(Group).Name} entity with ID {id}");
            }

            return group.ToModel();
        }

        public async Task<IEnumerable<Group>> GetAll()
        {
            var groups = await _authorizationDbContext.Groups
                .Include(g => g.GroupRoles)
                .ThenInclude(gr => gr.Role)
                .Include(g => g.GroupUsers)
                .ThenInclude(gu => gu.User)
                .Where(g => !g.IsDeleted)
                .ToArrayAsync();

            return groups.Select(g => g.ToModel());
        }

        public async Task Delete(Group model)
        {
            var group = await _authorizationDbContext.Groups
                .Include(g => g.GroupRoles)
                .Include(g => g.GroupUsers)
                .SingleOrDefaultAsync(g => g.GroupId.Equals(model.Id, StringComparison.OrdinalIgnoreCase));

            if (group == null)
            {
                throw new NotFoundException<Group>($"Could not find {typeof(Group).Name} entity with ID {model.Id}");
            }

            group.IsDeleted = true;

            foreach (var groupRole in group.GroupRoles)
            {
                groupRole.IsDeleted = true;
            }
            foreach (var groupUser in group.GroupUsers)
            {
                groupUser.IsDeleted = true;
            }

            await _authorizationDbContext.SaveChangesAsync();
        }

        public async Task Update(Group model)
        {
            var group = await _authorizationDbContext.Groups
                .Include(g => g.GroupRoles)
                .Include(g => g.GroupUsers)
                .SingleOrDefaultAsync(g => g.GroupId.Equals(model.Id, StringComparison.OrdinalIgnoreCase));

            if (group == null)
            {
                throw new NotFoundException<Group>($"Could not find {typeof(Group).Name} entity with ID {model.Id}");
            }

            model.ToEntity(group);

            _authorizationDbContext.Groups.Update(group);
            await _authorizationDbContext.SaveChangesAsync();
        }

        public async Task<bool> Exists(string id)
        {
            var group = await _authorizationDbContext.Groups
                .SingleOrDefaultAsync(g => g.GroupId.Equals(id, StringComparison.OrdinalIgnoreCase)
                                           && !g.IsDeleted);

            return group != null;
        }
    }
}
