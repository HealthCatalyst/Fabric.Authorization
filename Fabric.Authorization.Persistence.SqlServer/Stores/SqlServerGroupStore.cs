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
using Group = Fabric.Authorization.Domain.Models.Group;
using Role = Fabric.Authorization.Domain.Models.Role;
using User = Fabric.Authorization.Domain.Models.User;

namespace Fabric.Authorization.Persistence.SqlServer.Stores
{
    public class SqlServerGroupStore : SqlServerBaseStore, IGroupStore
    {
        public SqlServerGroupStore(IAuthorizationDbContext authorizationDbContext, IEventService eventService) : 
            base(authorizationDbContext, eventService)
        {
        }

        public async Task<Group> Add(Group group)
        {
            var alreadyExists = await Exists(group.Name);
            if (alreadyExists)
            {
                throw new AlreadyExistsException<Group>(
                    $"Group {group.Name} already exists. Please use a different GroupName.");
            }

            group.Id = Guid.NewGuid().ToString();
            var groupEntity = group.ToEntity();
            AuthorizationDbContext.Groups.Add(groupEntity);
            await AuthorizationDbContext.SaveChangesAsync();
            await EventService.RaiseEventAsync(new EntityAuditEvent<Group>(EventTypes.EntityCreatedEvent, group.Id, group));
            return groupEntity.ToModel();
        }

        public async Task<Group> Get(string name)
        {
            var group = await AuthorizationDbContext.Groups
                .Include(g => g.GroupRoles)
                .ThenInclude(gr => gr.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .ThenInclude(p => p.SecurableItem)
                .Include(g => g.GroupUsers)
                .ThenInclude(gu => gu.User)
                .ThenInclude(u => u.UserPermissions)
                .ThenInclude(up => up.Permission)
                .Include(g => g.GroupRoles)
                .ThenInclude(gr => gr.Role)
                .ThenInclude(r => r.SecurableItem)
                .AsNoTracking()
                .SingleOrDefaultAsync(g => g.Name == name
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
            var groups = await AuthorizationDbContext.Groups
                .Include(g => g.GroupRoles)
                .ThenInclude(gr => gr.Role)
                .Include(g => g.GroupUsers)
                .ThenInclude(gu => gu.User)
                .Where(g => !g.IsDeleted)
                .ToArrayAsync();

            return groups.Select(g => g.ToModel());
        }

        public async Task Delete(Group group)
        {
            var groupEntity = await AuthorizationDbContext.Groups
                .Include(g => g.GroupRoles)
                .Include(g => g.GroupUsers)
                .SingleOrDefaultAsync(g => g.GroupId == Guid.Parse(group.Id));

            if (groupEntity == null)
            {
                throw new NotFoundException<Group>($"Could not find {typeof(Group).Name} entity with ID {group.Name}");
            }

            groupEntity.IsDeleted = true;

            foreach (var groupRole in groupEntity.GroupRoles)
            {
                if (!groupRole.IsDeleted)
                {
                    groupRole.IsDeleted = true;
                }
            }

            foreach (var groupUser in groupEntity.GroupUsers)
            {
                if (!groupUser.IsDeleted)
                {
                    groupUser.IsDeleted = true;
                }
            }

            await AuthorizationDbContext.SaveChangesAsync();
            await EventService.RaiseEventAsync(new EntityAuditEvent<Group>(EventTypes.EntityDeletedEvent, group.Id, group));
        }

        public async Task Update(Group group)
        {
            var groupEntity = await AuthorizationDbContext.Groups
                .Include(g => g.GroupRoles)
                .Include(g => g.GroupUsers)
                .SingleOrDefaultAsync(g => g.GroupId == Guid.Parse(group.Id));

            if (groupEntity == null)
            {
                throw new NotFoundException<Group>($"Could not find {typeof(Group).Name} entity with ID {group.Name}");
            }

            group.ToEntity(groupEntity);

            AuthorizationDbContext.Groups.Update(groupEntity);
            await AuthorizationDbContext.SaveChangesAsync();
            await EventService.RaiseEventAsync(new EntityAuditEvent<Group>(EventTypes.EntityUpdatedEvent, group.Id, group));
        }

        public async Task<bool> Exists(string name)
        {
            var group = await AuthorizationDbContext.Groups
                .SingleOrDefaultAsync(g => g.Name == name
                                           && !g.IsDeleted).ConfigureAwait(false);

            return group != null;
        }

        public async Task<Group> AddRolesToGroup(Group group, IEnumerable<Role> rolesToAdd)
        {
            var groupEntity = await AuthorizationDbContext.Groups.SingleOrDefaultAsync(g =>
                g.Name == group.Name
                && !g.IsDeleted);

            if (groupEntity == null)
            {
                throw new NotFoundException<Group>($"Could not find {typeof(Group).Name} entity with ID {group.Name}");
            }

            var groupRoles = new List<GroupRole>();
            foreach (var role in rolesToAdd)
            {
                group.Roles.Add(role);
                var groupRole = new GroupRole
                {
                    GroupId = groupEntity.GroupId,
                    RoleId = role.Id
                };

                AuthorizationDbContext.GroupRoles.Add(groupRole);
                groupRoles.Add(groupRole);
            }

            await AuthorizationDbContext.SaveChangesAsync();
            await EventService.RaiseEventAsync(new EntityBatchAuditEvent<GroupRole>(EventTypes.EntityCreatedEvent, groupRoles));
            return group;
        }

        public async Task<Group> DeleteRolesFromGroup(Group group, IEnumerable<Guid> roleIdsToDelete)
        {
            var groupRolesToRemove = AuthorizationDbContext.GroupRoles
                .Where(gr => roleIdsToDelete.Contains(gr.RoleId) &&
                             gr.GroupId == Guid.Parse(group.Id)).ToList();

            if (groupRolesToRemove.Count == 0)
            {
                throw new NotFoundException<Role>(
                    $"No role mappings found for group {group.Name} with the supplied role IDs");
            }

            var missingRoleMappings = new List<Guid>();

            foreach (var groupRole in groupRolesToRemove)
            {
                // remove the role from the domain model
                var roleToRemove = group.Roles.FirstOrDefault(r => r.Id == groupRole.RoleId);

                if (roleToRemove == null)
                {
                    missingRoleMappings.Add(groupRole.RoleId);
                }

                group.Roles.Remove(roleToRemove);

                // mark the many-to-many DB entity as deleted
                groupRole.IsDeleted = true;
                AuthorizationDbContext.GroupRoles.Update(groupRole);
            }

            if (missingRoleMappings.Any())
            {
                throw new NotFoundException<Role>(
                    $"No role mapping(s) found for group {group.Name} with the following role IDs: {missingRoleMappings.ToString(", ")}");
            }

            await AuthorizationDbContext.SaveChangesAsync();
            await EventService.RaiseEventAsync(new EntityBatchAuditEvent<GroupRole>(EventTypes.EntityDeletedEvent, groupRolesToRemove));
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

            AuthorizationDbContext.GroupUsers.Add(groupUser);
            await AuthorizationDbContext.SaveChangesAsync();
            await EventService.RaiseEventAsync(new EntityAuditEvent<GroupUser>(EventTypes.EntityCreatedEvent, $"Group {groupUser.GroupId} / User {user.IdentityProvider}/{user.SubjectId}", groupUser));
            return group;
        }

        public async Task<Group> AddUsersToGroup(Group group, IEnumerable<User> usersToAdd)
        {
            var groupEntity = await AuthorizationDbContext.Groups.SingleOrDefaultAsync(g =>
                g.Name == group.Name
                && !g.IsDeleted);

            if (groupEntity == null)
            {
                throw new NotFoundException<Group>($"Could not find {typeof(Group).Name} entity with ID {group.Name}");
            }

            var groupUsers = new List<GroupUser>();
            foreach (var user in usersToAdd)
            {
                group.Users.Add(user);
                var groupUser = new GroupUser
                {
                    GroupId = groupEntity.GroupId,
                    IdentityProvider = user.IdentityProvider,
                    SubjectId = user.SubjectId
                };

                AuthorizationDbContext.GroupUsers.Add(groupUser);
                groupUsers.Add(groupUser);
            }

            await AuthorizationDbContext.SaveChangesAsync();
            await EventService.RaiseEventAsync(
                new EntityBatchAuditEvent<GroupUser>(EventTypes.EntityCreatedEvent, groupUsers));

            return group;
        }

        public async Task<Group> DeleteUserFromGroup(Group group, User user)
        {
            var groupUser = await AuthorizationDbContext.GroupUsers
                .SingleOrDefaultAsync(gu =>
                    gu.SubjectId == user.SubjectId &&
                    gu.IdentityProvider == user.IdentityProvider &&
                    gu.GroupId == Guid.Parse(group.Id));

            if (groupUser != null)
            {
                groupUser.IsDeleted = true;
                AuthorizationDbContext.GroupUsers.Update(groupUser);
                await AuthorizationDbContext.SaveChangesAsync();
                await EventService.RaiseEventAsync(new EntityAuditEvent<GroupUser>(EventTypes.EntityDeletedEvent, groupUser.Id.ToString(), groupUser));
            }

            return group;
        }
    }
}