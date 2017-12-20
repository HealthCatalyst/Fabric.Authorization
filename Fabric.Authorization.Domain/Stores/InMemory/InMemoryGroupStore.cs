using System;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores.Services;

namespace Fabric.Authorization.Domain.Stores.InMemory
{
    public class InMemoryGroupStore : InMemoryFormattableIdentifierStore<Group>, IGroupStore
    {
        private readonly IRoleStore _roleStore;
        private readonly IUserStore _userStore;

        [Obsolete]
        public InMemoryGroupStore(IIdentifierFormatter identifierFormatter, 
            IRoleStore roleStore,
            IUserStore userStore) 
            : base(identifierFormatter)
        {
            _roleStore = roleStore;
            _userStore = userStore;
            var group1 = new Group
            {
                Id = Guid.NewGuid().ToString(),
                Name = @"FABRIC\Health Catalyst Viewer",
            };

            var group2 = new Group
            {
                Id = Guid.NewGuid().ToString(),
                Name = @"FABRIC\Health Catalyst Editor",
            };

            this.Add(group1).Wait();
            this.Add(group2).Wait();
        }

        public override async Task<Group> Add(Group group)
        {
            group.Track();

            var formattedId = FormatId(group.Identifier);

            if (Dictionary.ContainsKey(formattedId))
            {
                var existingGroup = Dictionary[formattedId];
                if (existingGroup == null)
                {
                    throw new CouldNotCompleteOperationException();
                }
                if (!existingGroup.IsDeleted)
                {
                    throw new AlreadyExistsException<Group>(group, formattedId);
                }

                existingGroup.IsDeleted = false;
                await Update(existingGroup).ConfigureAwait(false);
                return existingGroup;
            }

            Dictionary.TryAdd(formattedId, group);
            return group;
        }

        public override async Task Delete(Group group)
        {
            group.IsDeleted = true;

            var formattedId = FormatId(group.Id);

            // use base class Exists so IsDeleted is ignored since it's already been updated at this point
            if (await base.Exists(formattedId).ConfigureAwait(false))
            {
                if (!Dictionary.TryUpdate(formattedId, group, Dictionary[formattedId]))
                {
                    throw new CouldNotCompleteOperationException();
                }
            }
            else
            {
                throw new NotFoundException<Group>(group, group.Identifier);
            }
        }

        public override async Task<bool> Exists(string id)
        {
            var formattedId = FormatId(id);
            return await base.Exists(formattedId) && !Dictionary[formattedId].IsDeleted;
        }

        public async Task<Group> AddRoleToGroup(string groupName, Guid roleId)
        {
            var group = await Get(groupName);
            var role = await _roleStore.Get(roleId);

            if (group.Roles.Any(r => r.Id == roleId)
                || role.Groups.Any(g => string.Equals(g, groupName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new AlreadyExistsException<Role>($"Role {role.Name} already exists for group {group.Name}. Please provide a new role id.");
            }

            group.Roles.Add(role);
            role.Groups.Add(groupName);

            await _roleStore.Update(role);
            await Update(group);

            return group;
        }

        public async Task<Group> DeleteRoleFromGroup(string groupName, Guid roleId)
        {
            var group = await Get(groupName);
            var role = await _roleStore.Get(roleId);

            var groupRole = group.Roles.FirstOrDefault(r => r.Id == roleId);
            if (groupRole != null)
            {
                group.Roles.Remove(groupRole);
            }

            if (role.Groups.Any(g => string.Equals(g, groupName, StringComparison.OrdinalIgnoreCase)))
            {
                role.Groups.Remove(groupName);
            }

            await _roleStore.Update(role);
            await Update(group);
            return group;
        }

        public async Task<Group> AddUserToGroup(string groupName, string subjectId, string identityProvider)
        {
            var group = await Get(groupName);

            //only add users to a custom group
            if (!string.Equals(group.Source, GroupConstants.CustomSource, StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestException<Group>("The group to which you are attempting to add a user is not specified as a 'Custom' group. Only 'Custom' groups allow associations with users.");
            }

            User user;
            try
            {
                user = await _userStore.Get($"{subjectId}:{identityProvider}");
            }
            catch (NotFoundException<User>)
            {
                user = await _userStore.Add(new User(subjectId, identityProvider));
            }

            if (!group.Users.Any(u =>
                string.Equals(u.SubjectId, subjectId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(u.IdentityProvider, identityProvider, StringComparison.OrdinalIgnoreCase)))
            {
                group.Users.Add(user);
            }
            else
            {
                throw new AlreadyExistsException<Group>($"The user {identityProvider}:{subjectId} has already been added to the group {groupName}.");
            }

            if (user.Groups.All(g => !string.Equals(g, groupName, StringComparison.OrdinalIgnoreCase)))
            {
                user.Groups.Add(groupName);
            }

            await _userStore.Update(user);
            await Update(group);
            return group;
        }

        public async Task<Group> DeleteUserFromGroup(string groupName, string subjectId, string identityProvider)
        {
            var group = await Get(groupName);
            var user = await _userStore.Get($"{subjectId}:{identityProvider}");

            var groupUser = group.Users.FirstOrDefault(u => u.Id == user.Id);
            if (groupUser != null)
            {
                group.Users.Remove(groupUser);
            }

            if (user.Groups.Any(g => string.Equals(g, groupName, StringComparison.OrdinalIgnoreCase)))
            {
                user.Groups.Remove(groupName);
            }

            await _userStore.Update(user);
            await Update(group);
            return group;
        }
    }
}