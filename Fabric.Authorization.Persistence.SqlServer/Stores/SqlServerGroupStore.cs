using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Domain.Stores.Services;
using Fabric.Authorization.Persistence.SqlServer.EntityModels;
using Fabric.Authorization.Persistence.SqlServer.Mappers;
using Fabric.Authorization.Persistence.SqlServer.Services;
using Microsoft.EntityFrameworkCore;
using Group = Fabric.Authorization.Domain.Models.Group;
using Role = Fabric.Authorization.Domain.Models.Role;

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
                .Include(g => g.GroupRoles.Where(gr => !gr.IsDeleted))
                .ThenInclude(gr => gr.Role)
                .Include(g => g.GroupUsers.Where(gu => !gu.IsDeleted))
                .ThenInclude(gu => gu.User)
                .SingleOrDefaultAsync(g => g.Name.Equals(id, StringComparison.OrdinalIgnoreCase)
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
                .Include(g => g.GroupRoles.Where(gr => !gr.IsDeleted))
                .ThenInclude(gr => gr.Role)
                .Include(g => g.GroupUsers.Where(gu => !gu.IsDeleted))
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
                .SingleOrDefaultAsync(g => g.GroupId.Equals(model.Name, StringComparison.OrdinalIgnoreCase));

            if (group == null)
            {
                throw new NotFoundException<Group>($"Could not find {typeof(Group).Name} entity with ID {model.Name}");
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
                .SingleOrDefaultAsync(g => g.GroupId.Equals(model.Name, StringComparison.OrdinalIgnoreCase));

            if (group == null)
            {
                throw new NotFoundException<Group>($"Could not find {typeof(Group).Name} entity with ID {model.Name}");
            }

            model.ToEntity(group);

            _authorizationDbContext.Groups.Update(group);
            await _authorizationDbContext.SaveChangesAsync();
        }

        public async Task<bool> Exists(string id)
        {
            var group = await _authorizationDbContext.Groups
                .SingleOrDefaultAsync(g => g.Name.Equals(id, StringComparison.OrdinalIgnoreCase)
                                           && !g.IsDeleted);

            return group != null;
        }

        public async Task<Group> AddRoleToGroup(string groupName, Guid roleId)
        {           
            var group = await _authorizationDbContext.Groups
                .SingleOrDefaultAsync(g => g.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase));

            if (group == null)
            {
                throw new NotFoundException<Group>($"Could not find {typeof(Group).Name} entity with ID {groupName}");
            }

            var role = await _authorizationDbContext.Roles
                .SingleOrDefaultAsync(r => r.RoleId.Equals(roleId));

            if (role == null)
            {
                throw new NotFoundException<Role>($"Could not find {typeof(Role).Name} entity with ID {roleId}");
            }
         
            if (group.GroupRoles.SingleOrDefault(gr => gr.RoleId.Equals(role.Id)) != null)
            {
                throw new AlreadyExistsException<Role>($"Role {role.Name} already exists for group {group.Name}. Please provide a new role id.");
            }

            var groupRole = new GroupRole
            {
                GroupId = group.Id,
                RoleId = role.RoleId
            };
            group.GroupRoles.Add(groupRole);
            _authorizationDbContext.GroupRoles.Add(groupRole);

            var groupModel = group.ToModel();
            return groupModel;
        }

        public async Task<Group> DeleteRoleFromGroup(string groupName, Guid roleId)
        {
            var group = await _authorizationDbContext.Groups
                .Include(g => g.GroupRoles)
                .SingleOrDefaultAsync(g => g.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase));

            if (group == null)
            {
                throw new NotFoundException<Group>($"Could not find {typeof(Group).Name} entity with ID {groupName}");
            }

            var role = await _authorizationDbContext.Roles
                .SingleOrDefaultAsync(r => r.RoleId.Equals(roleId));

            if (role == null)
            {
                throw new NotFoundException<Role>($"Could not find {typeof(Role).Name} entity with ID {roleId}");
            }

            var groupRoleToRemove = group.GroupRoles.SingleOrDefault(r => r.RoleId.Equals(role.Id));

            if (groupRoleToRemove != null)
            {
                groupRoleToRemove.IsDeleted = true;
                group.GroupRoles.Remove(groupRoleToRemove);
                _authorizationDbContext.GroupRoles.Update(groupRoleToRemove);
                await _authorizationDbContext.SaveChangesAsync();
            }

            var groupModel = group.ToModel();
            return groupModel;
        }

        public async Task<Group> AddUserToGroup(string groupName, string subjectId, string identityProvider)
        {
            var group = await _authorizationDbContext.Groups
                .Include(g => g.GroupUsers)
                .SingleOrDefaultAsync(g =>
                    g.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase));

            if (group == null)
            {
                throw new NotFoundException<Group>($"Could not find {typeof(Group).Name} entity with ID {groupName}");
            }

            //only add users to a custom group
            if (!string.Equals(group.Source, GroupConstants.CustomSource, StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestException<Group>("The group to which you are attempting to add a user is not specified as a 'Custom' group. Only 'Custom' groups allow associations with users.");
            }

            //see if user exists and if not then add the user 
            var user = await _authorizationDbContext.Users.SingleOrDefaultAsync(u =>
                u.SubjectId.Equals(subjectId, StringComparison.OrdinalIgnoreCase)
                && u.IdentityProvider.Equals(identityProvider, StringComparison.OrdinalIgnoreCase));

            if (user == null)
            {
                user = new User
                {
                    SubjectId = subjectId,
                    IdentityProvider = identityProvider
                };
                _authorizationDbContext.Users.Add(user);
                await _authorizationDbContext.SaveChangesAsync();
            }

            //check if user already belongs to the group
            if (!group.GroupUsers.Any(u => u.UserId.Equals(user.Id)))
            {
                group.GroupUsers.Add(new GroupUser
                {
                    GroupId = group.Id,
                    UserId = user.Id
                });

                await _authorizationDbContext.SaveChangesAsync();
            }
            else
            {
                throw new AlreadyExistsException<Group>(
                    $"The user {identityProvider}:{subjectId} has already been added to the group {groupName}.");
            }

            return group.ToModel();
        }

        public async Task<Group> DeleteUserFromGroup(string groupName, string subjectId, string identityProvider)
        {
            var group = await _authorizationDbContext.Groups
                .Include(g => g.GroupUsers)
                .ThenInclude(gu => gu.User)
                .SingleOrDefaultAsync(g => g.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase));

            if (group == null)
            {
                throw new NotFoundException<Group>($"Could not find {typeof(Group).Name} entity with ID {groupName}");
            }

            var userInGroup = group.GroupUsers.SingleOrDefault(gu =>
                gu.User.SubjectId.Equals(subjectId, StringComparison.OrdinalIgnoreCase)
                && gu.User.IdentityProvider.Equals(identityProvider, StringComparison.OrdinalIgnoreCase));

            if (userInGroup != null)
            {
                userInGroup.IsDeleted = true;
                group.GroupUsers.Remove(userInGroup);
                await _authorizationDbContext.SaveChangesAsync();
            }

            return group.ToModel();
        }
    }
}
