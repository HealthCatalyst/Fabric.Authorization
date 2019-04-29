using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
            var groupNameMatches = AuthorizationDbContext.Groups
                .Where(g => !g.IsDeleted
                            && g.Name == group.GroupIdentifier.GroupName).Select(e => e.ToModel()).ToList();

            if (groupNameMatches.Any())
            {
                if (group.SourceEquals(GroupConstants.CustomSource))
                {
                    throw new AlreadyExistsException<Group>(
                        $"Could not create group name {group.Name}. A group with the same name exists as a Custom group or a Directory group.");
                }

                if (groupNameMatches.Any(g =>
                    new GroupIdentifierComparer().Equals(g.GroupIdentifier, group.GroupIdentifier)))
                {
                    throw new AlreadyExistsException<Group>(
                        $"Group {group.GroupIdentifier} already exists. Please use a different GroupName, IdentityProvider, or TenantId.");
                }
            }

            group.Id = Guid.NewGuid();
            var groupEntity = group.ToEntity();
            AuthorizationDbContext.Groups.Add(groupEntity);
            await AuthorizationDbContext.SaveChangesAsync();
            await EventService.RaiseEventAsync(new EntityAuditEvent<Group>(EventTypes.EntityCreatedEvent, group.Id.ToString(), groupEntity.ToModel()));
            return groupEntity.ToModel();
        }

        public async Task<IEnumerable<Group>> Add(IEnumerable<Group> groups)
        {
            var savedEntities = new List<Group>();
            var groupList = groups.ToList();
            var eventList = new List<EntityAuditEvent<Group>>();

            foreach (var group in groupList)
            {
                group.Id = Guid.NewGuid();
                var groupEntity = group.ToEntity();
                AuthorizationDbContext.Groups.Add(groupEntity);
                savedEntities.Add(group);
                eventList.Add(new EntityAuditEvent<Group>(EventTypes.EntityCreatedEvent, group.Id.ToString(), group));
            }

            await AuthorizationDbContext.SaveChangesAsync();

            if (eventList.Count > 0)
            {
                await EventService.RaiseEventsAsync(eventList);
            }
            return savedEntities;
        }

        public async Task<Group> Get(Guid id)
        {
            var groupEntity = await AuthorizationDbContext.Groups
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
                .Include(g => g.ParentGroups)
                .ThenInclude(cg => cg.Parent)
                .Include(g => g.ChildGroups)
                .ThenInclude(cg => cg.Child)
                .AsNoTracking()
                .SingleOrDefaultAsync(g => g.GroupId == id && !g.IsDeleted);

            if (groupEntity == null)
            {
                throw new NotFoundException<Group>($"Could not find {typeof(Group).Name} entity with ID {id}");
            }

            groupEntity.GroupRoles = groupEntity.GroupRoles.Where(gr => !gr.IsDeleted).ToList();
            foreach (var groupRole in groupEntity.GroupRoles)
            {
                groupRole.Role.RolePermissions = groupRole.Role.RolePermissions.Where(rp => !rp.IsDeleted).ToList();
            }

            groupEntity.GroupUsers = groupEntity.GroupUsers.Where(gu => !gu.IsDeleted).ToList();
            foreach (var groupUser in groupEntity.GroupUsers)
            {
                groupUser.User.UserPermissions = groupUser.User.UserPermissions.Where(up => !up.IsDeleted).ToList();
            }

            groupEntity.ChildGroups = groupEntity.ChildGroups.Where(cg => !cg.IsDeleted).ToList();
            groupEntity.ParentGroups = groupEntity.ParentGroups.Where(pg => !pg.IsDeleted).ToList();

            return groupEntity.ToModel();
        }

        public async Task<Group> Get(GroupIdentifier groupIdentifier)
        {
            var groupEntities = AuthorizationDbContext.Groups
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
                .Include(g => g.ParentGroups)
                .ThenInclude(cg => cg.Parent)
                .Include(g => g.ChildGroups)
                .ThenInclude(cg => cg.Child)
                .ThenInclude(cg => cg.GroupRoles)
                .ThenInclude(gr => gr.Role)
                .AsNoTracking()
                .Where(g => !g.IsDeleted && g.Name == groupIdentifier.GroupName)
                .ToList();

            EntityModels.Group groupEntity = null;
            if (groupEntities.Count == 0)
            {
                throw new NotFoundException<Group>($"Could not find {typeof(Group).Name} entity with Identifier {groupIdentifier}");
            }

            if (groupEntities.Count == 1)
            {
                if (string.Equals(groupEntities[0].Source, GroupConstants.CustomSource, StringComparison.OrdinalIgnoreCase))
                {
                    groupEntity = groupEntities[0];
                }
                else
                {
                    groupEntity = groupEntities.FirstOrDefault(g =>
                        string.Equals(g.IdentityProvider, groupIdentifier.IdentityProvider, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(g.TenantId, groupIdentifier.TenantId, StringComparison.OrdinalIgnoreCase));
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(groupIdentifier.TenantId))
                {
                    groupEntity = groupEntities.FirstOrDefault(g =>
                        string.Equals(g.IdentityProvider, groupIdentifier.IdentityProvider, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(g.TenantId, groupIdentifier.TenantId, StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    groupEntity = groupEntities.FirstOrDefault(g =>
                        string.Equals(g.Source, GroupConstants.CustomSource, StringComparison.OrdinalIgnoreCase)
                        || (string.Equals(g.IdentityProvider, groupIdentifier.IdentityProvider, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(g.TenantId, groupIdentifier.TenantId, StringComparison.OrdinalIgnoreCase)));
                }
            }

            if (groupEntity == null)
            {
                throw new NotFoundException<Group>($"Could not find {typeof(Group).Name} entity with Identifier {groupIdentifier}");
            }

            groupEntity.GroupRoles = groupEntity.GroupRoles.Where(gr => !gr.IsDeleted).ToList();
            foreach (var groupRole in groupEntity.GroupRoles)
            {
                groupRole.Role.RolePermissions = groupRole.Role.RolePermissions.Where(rp => !rp.IsDeleted).ToList();
            }

            groupEntity.GroupUsers = groupEntity.GroupUsers.Where(gu => !gu.IsDeleted).ToList();
            foreach (var groupUser in groupEntity.GroupUsers)
            {
                groupUser.User.UserPermissions = groupUser.User.UserPermissions.Where(up => !up.IsDeleted).ToList();
            }

            groupEntity.ChildGroups = groupEntity.ChildGroups.Where(cg => !cg.IsDeleted).ToList();
            groupEntity.ParentGroups = groupEntity.ParentGroups.Where(pg => !pg.IsDeleted).ToList();
            
            return groupEntity.ToModel();
        }

        public async Task<IEnumerable<Group>> GetAll()
        {
            var groupEntities = await AuthorizationDbContext.Groups
                .Include(g => g.GroupRoles)
                    .ThenInclude(gr => gr.Role)
                        .ThenInclude(r => r.SecurableItem)
                .Include(g => g.GroupRoles)
                    .ThenInclude(gr => gr.Role)
                        .ThenInclude(r => r.RolePermissions)
                .Include(g => g.GroupUsers)
                    .ThenInclude(gu => gu.User)
                        .ThenInclude(u => u.UserPermissions)
                .Include(g => g.ParentGroups)
                    .ThenInclude(cg => cg.Parent)
                .Include(g => g.ChildGroups)
                    .ThenInclude(cg => cg.Child)
                .Where(g => !g.IsDeleted)
                .ToArrayAsync();

            foreach (var groupEntity in groupEntities)
            {
                groupEntity.GroupRoles = groupEntity.GroupRoles.Where(gr => !gr.IsDeleted).ToList();
                foreach (var groupRole in groupEntity.GroupRoles)
                {
                    groupRole.Role.RolePermissions = groupRole.Role.RolePermissions.Where(rp => !rp.IsDeleted).ToList();
                }
            }

            foreach (var groupEntity in groupEntities)
            {
                groupEntity.GroupUsers = groupEntity.GroupUsers.Where(gu => !gu.IsDeleted).ToList();
            }

            foreach (var groupEntity in groupEntities)
            {
                foreach (var groupUser in groupEntity.GroupUsers)
                {
                    groupUser.User.UserPermissions = groupUser.User.UserPermissions.Where(up => !up.IsDeleted).ToList();
                }
            }

            foreach (var groupEntity in groupEntities)
            {
                groupEntity.ChildGroups = groupEntity.ChildGroups.Where(cg => !cg.IsDeleted).ToList();
                groupEntity.ParentGroups = groupEntity.ParentGroups.Where(pg => !pg.IsDeleted).ToList();
            }

            return groupEntities.Select(g => g.ToModel());
        }

        public async Task Delete(Group group)
        {
            var groupEntity = await AuthorizationDbContext.Groups
                .Include(g => g.GroupRoles)
                .Include(g => g.GroupUsers)
                .Include(g => g.ChildGroups)
                .Include(g => g.ParentGroups)
                .SingleOrDefaultAsync(g => g.GroupId == group.Id);

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

            foreach (var childGroup in groupEntity.ChildGroups)
            {
                if (!childGroup.IsDeleted)
                {
                    childGroup.IsDeleted = true;
                }
            }

            foreach (var parentGroup in groupEntity.ParentGroups)
            {
                if (!parentGroup.IsDeleted)
                {
                    parentGroup.IsDeleted = true;
                }
            }

            await AuthorizationDbContext.SaveChangesAsync();
            await EventService.RaiseEventAsync(new EntityAuditEvent<Group>(EventTypes.EntityDeletedEvent, group.Id.ToString(), groupEntity.ToModel()));
        }

        public async Task Update(Group group)
        {
            var groupEntity = await AuthorizationDbContext.Groups
                .Include(g => g.GroupRoles)
                .Include(g => g.GroupUsers)
                .SingleOrDefaultAsync(g => g.GroupId == group.Id);

            if (groupEntity == null)
            {
                throw new NotFoundException<Group>($"Could not find {typeof(Group).Name} entity with ID {group.Name}");
            }

            group.ToEntity(groupEntity);

            AuthorizationDbContext.Groups.Update(groupEntity);
            await AuthorizationDbContext.SaveChangesAsync();
            await EventService.RaiseEventAsync(new EntityAuditEvent<Group>(EventTypes.EntityUpdatedEvent, group.Id.ToString(), groupEntity.ToModel()));
        }

        public async Task<bool> Exists(Guid id)
        {
            var group = await AuthorizationDbContext.Groups
                .SingleOrDefaultAsync(g => g.GroupId == id
                                           && !g.IsDeleted).ConfigureAwait(false);

            return group != null;
        }

        public Task<bool> Exists(GroupIdentifier groupIdentifier)
        {
            var groups = AuthorizationDbContext.Groups
                .Where(g => !g.IsDeleted
                && g.Name == groupIdentifier.GroupName).Select(e => e.ToModel()).ToList();

            return Task.FromResult(groups.Any(g => g.SourceEquals(GroupConstants.CustomSource)
                                   || (g.SourceEquals(GroupConstants.DirectorySource)
                                       && g.IdentityProviderEquals(groupIdentifier.IdentityProvider)
                                       && g.TenantIdEquals(groupIdentifier.TenantId))));
        }

        public async Task<Group> AddRolesToGroup(Group group, IEnumerable<Role> rolesToAdd)
        {
            var groupEntity = await AuthorizationDbContext.Groups.SingleOrDefaultAsync(g =>
                g.Name == group.Name
                && g.TenantId == group.TenantId
                && g.IdentityProvider == group.IdentityProvider
                && !g.IsDeleted);

            if (groupEntity == null)
            {
                throw new NotFoundException<Group>($"Could not find {typeof(Group).Name} entity with ID {group.Name}");
            }

            foreach (var role in rolesToAdd)
            {
                group.Roles.Add(role);
                AuthorizationDbContext.GroupRoles.Add(new GroupRole
                {
                    GroupId = groupEntity.GroupId,
                    RoleId = role.Id
                });
            }

            await AuthorizationDbContext.SaveChangesAsync();
            await EventService.RaiseEventAsync(new EntityAuditEvent<Group>(EventTypes.ChildEntityCreatedEvent, group.Id.ToString(), group));
            return group;
        }

        public async Task<Group> DeleteRolesFromGroup(Group group, IEnumerable<Guid> roleIdsToDelete)
        {
            var groupRolesToRemove = AuthorizationDbContext.GroupRoles
                .Where(gr => roleIdsToDelete.Contains(gr.RoleId)
                             && gr.GroupId == group.Id
                             && !gr.IsDeleted).ToList();

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
            await EventService.RaiseEventAsync(new EntityAuditEvent<Group>(EventTypes.ChildEntityDeletedEvent, group.Id.ToString(), group));
            return group;
        }

        public async Task<Group> AddUsersToGroup(Group group, IEnumerable<User> usersToAdd)
        {
            var groupEntity = await AuthorizationDbContext.Groups.SingleOrDefaultAsync(g =>
                g.Name == group.Name
                && g.TenantId == group.TenantId
                && g.IdentityProvider == group.IdentityProvider
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
            await EventService.RaiseEventAsync(new EntityAuditEvent<Group>(EventTypes.ChildEntityCreatedEvent, group.Id.ToString(), group));

            return group;
        }

        public async Task<Group> DeleteUserFromGroup(Group group, User user)
        {
            var groupUser = await AuthorizationDbContext.GroupUsers
                .SingleOrDefaultAsync(gu =>
                    gu.SubjectId == user.SubjectId &&
                    gu.IdentityProvider == user.IdentityProvider &&
                    gu.GroupId == group.Id && !gu.IsDeleted);

            if (groupUser == null)
            {
                return group;
            }

            groupUser.IsDeleted = true;
            AuthorizationDbContext.GroupUsers.Update(groupUser);
            await AuthorizationDbContext.SaveChangesAsync();
            await EventService.RaiseEventAsync(new EntityAuditEvent<Group>(EventTypes.ChildEntityDeletedEvent,
                groupUser.Id.ToString(), group));

            return group;
        }

        public async Task<Group> AddChildGroups(Group group, IEnumerable<Group> childGroups)
        {
            foreach (var childGroup in childGroups)
            {
                var newChildGroup = new ChildGroup
                {
                    ParentGroupId = group.Id,
                    ChildGroupId = childGroup.Id
                };

                AuthorizationDbContext.ChildGroups.Add(newChildGroup);
                group.Children.Add(childGroup);
            }

            await AuthorizationDbContext.SaveChangesAsync();
            await EventService.RaiseEventAsync(new EntityAuditEvent<Group>(EventTypes.ChildEntityCreatedEvent, group.Id.ToString(), group));

            return group;
        }

        public async Task<Group> RemoveChildGroups(Group group, IEnumerable<Group> childGroups)
        {
            var childGroupList = childGroups.ToList();
            var childGroupIds = childGroupList.Select(g => g.Id);
            var childGroupEntities = AuthorizationDbContext.ChildGroups.Where(cg => !cg.IsDeleted && cg.ParentGroupId == group.Id && childGroupIds.Contains(cg.ChildGroupId));

            foreach (var childGroup in childGroupList)
            {
                var childToRemove = group.Children.FirstOrDefault(c => c.Name == childGroup.Name);
                if (childToRemove == null)
                {
                    continue;
                }

                group.Children.Remove(childToRemove);

                var childToRemoveEntity = childGroupEntities.FirstOrDefault(cg => cg.ParentGroupId == group.Id && cg.ChildGroupId == childGroup.Id);

                if (childToRemoveEntity != null)
                {
                    childToRemoveEntity.IsDeleted = true;
                }
            }

            await AuthorizationDbContext.SaveChangesAsync();
            await EventService.RaiseEventAsync(new EntityAuditEvent<Group>(EventTypes.ChildEntityDeletedEvent, group.Id.ToString(), group));

            return group;
        }

        public Task<IEnumerable<Group>> Get(IEnumerable<GroupIdentifier> groupIdentifiers, bool ignoreMissingGroups = false)
        {
            var groupEntities = AuthorizationDbContext.Groups
                .Include(g => g.ChildGroups)
                .ThenInclude(cg => cg.Child)
                .Include(g => g.ParentGroups)
                .ThenInclude(pg => pg.Parent)
                .Where(g =>
                    !g.IsDeleted)
                .ToList();

            var filteredEntities = new List<EntityModels.Group>();
            var missingGroupIdentifiers = new List<GroupIdentifier>();
            foreach (var identifier in groupIdentifiers)
            {

                var entity = groupEntities.FirstOrDefault(g =>
                    string.Equals(g.Name, identifier.GroupName, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(g.TenantId, identifier.TenantId, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(g.IdentityProvider, identifier.IdentityProvider,
                        StringComparison.OrdinalIgnoreCase));

                if (entity != null)
                {
                    filteredEntities.Add(entity);
                }
                else
                {
                    missingGroupIdentifiers.Add(identifier);
                }
            }

            foreach (var groupEntity in filteredEntities)
            {
                groupEntity.ChildGroups = groupEntity.ChildGroups.Where(cg => !cg.IsDeleted).ToList();
                groupEntity.ParentGroups = groupEntity.ParentGroups.Where(pg => !pg.IsDeleted).ToList();
            }

            if (!ignoreMissingGroups)
            {
                if (missingGroupIdentifiers.Count > 0)
                {
                    throw new NotFoundException<Group>(
                        $"The following groups could not be found: {string.Join(",", missingGroupIdentifiers)}",
                        missingGroupIdentifiers.Select(g => new NotFoundExceptionDetail {Identifier = g.ToString()})
                            .ToList());
                }
            }

            return Task.FromResult(filteredEntities.Select(g => g.ToModel()).AsEnumerable());
        }

        public Task<IEnumerable<Group>> GetGroupsByIdentifiers(IEnumerable<string> identifiers)
        {
            var identifierList = identifiers.ToList();
            try
            {
                var groupEntities = AuthorizationDbContext.Groups
                    .Include(g => g.ChildGroups)
                    .ThenInclude(cg => cg.Child)
                    .Include(g => g.ParentGroups)
                    .ThenInclude(pg => pg.Parent)
                    .Where(g =>
                        !g.IsDeleted
                        && (identifierList.Contains(g.Name) || identifierList.Contains(g.ExternalIdentifier)))
                    .ToList();

                foreach (var groupEntity in groupEntities)
                {
                    groupEntity.ChildGroups = groupEntity.ChildGroups.Where(cg => !cg.IsDeleted).ToList();
                    groupEntity.ParentGroups = groupEntity.ParentGroups.Where(pg => !pg.IsDeleted).ToList();
                }

                return Task.FromResult(groupEntities.Select(g => g.ToModel()).AsEnumerable());
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public async Task<IEnumerable<Group>> GetGroups(string name, string type)
        {
            var groupEntities = await AuthorizationDbContext.Groups
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
                .Where(g => g.Name.StartsWith(name)
                            && !g.IsDeleted
                            )
                .Where(GetGroupSourceFilter(type))
                .ToArrayAsync();

            return groupEntities.Select(g => g.ToModel());
        }

        private Expression<Func<EntityModels.Group, bool>> GetGroupSourceFilter(string source)
        {
            source = source ?? string.Empty;
            if (source.Equals(GroupConstants.CustomSource, StringComparison.OrdinalIgnoreCase))
            {
                return group => group.Source == "custom";
            }
            if (source.Equals(GroupConstants.DirectorySource, StringComparison.OrdinalIgnoreCase))
            {
                return group => group.Source != "custom";
            }
            return group => true;
        }
    }
}