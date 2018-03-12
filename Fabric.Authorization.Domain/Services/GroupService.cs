using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;

namespace Fabric.Authorization.Domain.Services
{
    public class GroupService
    {
        // TODO: move this out of GroupService to another library
        public static readonly Func<Role, string, string, bool> RoleFilter = (role, grain, securableItem) =>
        {
            var match = true;

            if (!string.IsNullOrWhiteSpace(grain))
            {
                match = role.Grain == grain;
            }

            if (match && !string.IsNullOrWhiteSpace(securableItem))
            {
                match = role.SecurableItem == securableItem;
            }

            return match;
        };

        private readonly IRoleStore _roleStore;
        private readonly IUserStore _userStore;
        private readonly IGroupStore _groupStore;
        private readonly RoleService _roleService;

        public GroupService(
            IGroupStore groupStore,
            IRoleStore roleStore,
            IUserStore userStore,
            RoleService roleService)
        {
            _roleStore = roleStore;
            _userStore = userStore;
            _groupStore = groupStore ?? throw new ArgumentNullException(nameof(groupStore));          
            _roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
        }

        public async Task<Group> AddGroup(Group group)
        {
            return await _groupStore.Add(group);
        }

        public async Task<Group> GetGroup(string id, string clientId)
        {
            var group = await _groupStore.Get(id);
            var clientRoles = (await _roleService.GetRoles(clientId)).ToList();
            group.Roles = clientRoles.Intersect(group.Roles).ToList();
            return group;
        }

        public async Task DeleteGroup(Group group)
        {
            await _groupStore.Delete(group);
        }

        public async Task UpdateGroupList(IEnumerable<Group> groups)
        {
            var allGroups = (await _groupStore.GetAll() ?? Enumerable.Empty<Group>()).ToList();

            var groupList = groups.ToList();

            var groupNames = groupList.Select(g => g.Name);
            var storedGroupNames = allGroups.Select(g => g.Name);

            var toDelete = allGroups.Where(g => !groupNames.Contains(g.Name, StringComparer.OrdinalIgnoreCase)).ToList();
            var toAdd = groupList.Where(g => !storedGroupNames.Contains(g.Name, StringComparer.OrdinalIgnoreCase)).ToList();

            foreach (var g in toDelete)
            {
                await _groupStore.Delete(g);
            }

            foreach (var g in toAdd)
            {
                await _groupStore.Add(g);
            }
        }

        public async Task<bool> Exists(string id)
        {
            return await _groupStore.Exists(id).ConfigureAwait(false);
        }

        public async Task<Group> AddRoleToGroup(string groupName, Guid roleId)
        {
            var group = await _groupStore.Get(groupName);
            var role = await _roleStore.Get(roleId);

            if (group.Roles.Any(r => r.Id == roleId)
                || role.Groups.Any(g => string.Equals(g, groupName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new AlreadyExistsException<Role>($"Role {role.Name} already exists for group {group.Name}. Please provide a new role id.");
            }

            return await _groupStore.AddRoleToGroup(group, role);
        }

        public async Task<Group> DeleteRoleFromGroup(string groupName, Guid roleId)
        {
            var group = await _groupStore.Get(groupName);
            var role = await _roleStore.Get(roleId);

            var groupRole = group.Roles.FirstOrDefault(r => r.Id == role.Id);
            if (groupRole != null)
            {
                group.Roles.Remove(groupRole);
            }

            return await _groupStore.DeleteRoleFromGroup(group, role);
        }

        public async Task<Group> AddUserToGroup(string groupName, string subjectId, string identityProvider)
        {
            var group = await _groupStore.Get(groupName);

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

            return await _groupStore.AddUserToGroup(group, user);
        }

        public async Task<Group> DeleteUserFromGroup(string groupName, string subjectId, string identityProvider)
        {
            var group = await _groupStore.Get(groupName);
            var user = await _userStore.Get($"{subjectId}:{identityProvider}");

            var groupUser = group.Users.FirstOrDefault(u => u.Id == user.Id);
            if (groupUser != null)
            {
                group.Users.Remove(groupUser);
            }

            return await _groupStore.DeleteUserFromGroup(group, user);
        }
    }
}