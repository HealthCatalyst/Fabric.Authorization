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
using Group = Fabric.Authorization.Domain.Models.Group;
using Role = Fabric.Authorization.Domain.Models.Role;
using User = Fabric.Authorization.Domain.Models.User;

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
            var alreadyExists = await Exists(model.Name);
            if (alreadyExists)
            {
                throw new AlreadyExistsException<Group>($"Group {model.Name} already exists. Please use a different GroupName.");
            }

            /*if (!string.IsNullOrEmpty(model.Id))
            {
                throw new BadRequestException<Group>("Id is generated internally and cannot be supplied by the caller.");
            }*/

            model.Id = Guid.NewGuid().ToString();
            var groupEntity = model.ToEntity();
            _authorizationDbContext.Groups.Add(groupEntity);
            await _authorizationDbContext.SaveChangesAsync();
            return groupEntity.ToModel();
        }

        public async Task<Group> Get(string name)
        {
            var group = await _authorizationDbContext.Groups
                .Include(g => g.GroupRoles)
                .ThenInclude(gr => gr.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .Include(g => g.GroupUsers)
                .ThenInclude(gu => gu.User)
                .ThenInclude(u => u.UserPermissions)
                .ThenInclude(up => up.Permission)
                .SingleOrDefaultAsync(g => g.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                                           && !g.IsDeleted);

            if (group == null)
            {
                throw new NotFoundException<Group>($"Could not find {typeof(Group).Name} entity with ID {name}");
            }

            group.GroupRoles = group.GroupRoles.Where(gr => !gr.IsDeleted).ToList();
            foreach (var groupRole in group.GroupRoles)
            {
                groupRole.Role.RolePermissions = groupRole.Role.RolePermissions.Where(rp => !rp.IsDeleted).ToList();
            }

            group.GroupUsers = group.GroupUsers.Where(gu => !gu.IsDeleted).ToList();
            foreach (var groupUser in group.GroupUsers)
            {
                groupUser.User.UserPermissions = groupUser.User.UserPermissions.Where(up => !up.IsDeleted).ToList();
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
                .SingleOrDefaultAsync(g => g.GroupId == Guid.Parse(model.Id));

            if (group == null)
            {
                throw new NotFoundException<Group>($"Could not find {typeof(Group).Name} entity with ID {model.Name}");
            }

            group.IsDeleted = true;

            foreach (var groupRole in group.GroupRoles)
            {
                if (!groupRole.IsDeleted)
                {
                    groupRole.IsDeleted = true;
                }
            }
            foreach (var groupUser in group.GroupUsers)
            {
                if (!groupUser.IsDeleted)
                {
                    groupUser.IsDeleted = true;
                }
            }

            await _authorizationDbContext.SaveChangesAsync();
        }

        public async Task Update(Group model)
        {
            var group = await _authorizationDbContext.Groups
                .Include(g => g.GroupRoles)
                .Include(g => g.GroupUsers)
                .SingleOrDefaultAsync(g => g.GroupId == Guid.Parse(model.Id));

            if (group == null)
            {
                throw new NotFoundException<Group>($"Could not find {typeof(Group).Name} entity with ID {model.Name}");
            }

            model.ToEntity(group);

            _authorizationDbContext.Groups.Update(group);
            await _authorizationDbContext.SaveChangesAsync();
        }

        public async Task<bool> Exists(string name)
        {
            var group = await _authorizationDbContext.Groups
                .SingleOrDefaultAsync(g => g.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                                           && !g.IsDeleted);

            return group != null;
        }

        public async Task<Group> AddRoleToGroup(Group group, Role role)
        {           
            var groupRole = new GroupRole
            {
                GroupId = Guid.Parse(group.Id),
                RoleId = role.Id
            };
            group.Roles.Add(role);

            _authorizationDbContext.GroupRoles.Add(groupRole);
            await _authorizationDbContext.SaveChangesAsync();
            
            return group;
        }

        public async Task<Group> DeleteRoleFromGroup(Group group, Role role)
        {
            var groupRoleToRemove = await _authorizationDbContext.GroupRoles
                .SingleOrDefaultAsync(gr => gr.RoleId.Equals(role.Id) &&
                                            gr.GroupId == Guid.Parse(group.Id));

            if (groupRoleToRemove != null)
            {
                groupRoleToRemove.IsDeleted = true;
                _authorizationDbContext.GroupRoles.Update(groupRoleToRemove);
                await _authorizationDbContext.SaveChangesAsync();
            }

            return group;
        }

        public async Task<Group> AddUserToGroup(Group group, User user)
        {
            var groupUser = new GroupUser
            {
                GroupId = Guid.Parse(group.Id),
                SubjectId = user.SubjectId,
                IdentityProvider = user.IdentityProvider
            };

            _authorizationDbContext.GroupUsers.Add(groupUser);
            await _authorizationDbContext.SaveChangesAsync();

            return group;
        }

        public async Task<Group> DeleteUserFromGroup(Group group, User user)
        {
            var userInGroup = await _authorizationDbContext.GroupUsers
                .SingleOrDefaultAsync(gu =>
                    gu.SubjectId.Equals(user.SubjectId, StringComparison.OrdinalIgnoreCase) &&
                    gu.IdentityProvider.Equals(user.IdentityProvider, StringComparison.OrdinalIgnoreCase) &&
                    gu.GroupId == Guid.Parse(group.Id));

            if (userInGroup != null)
            {
                userInGroup.IsDeleted = true;
                _authorizationDbContext.GroupUsers.Update(userInGroup);
                await _authorizationDbContext.SaveChangesAsync();
            }

            return group;
        }
    }
}
