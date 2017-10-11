using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores.Services
{
    public class GroupService
    {
        // TODO: move this out of GroupService to another library
        public static readonly Func<Role, string, string, bool> GroupRoleFilter = (role, grain, securableItem) =>
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

        private readonly IGroupStore _groupStore;
        private readonly IRoleStore _roleStore;
        private readonly IUserStore _userStore;
        private readonly RoleService _roleService;

        public GroupService(
            IGroupStore groupStore,
            IRoleStore roleStore,
            IUserStore userStore,
            RoleService roleService)
        {
            _groupStore = groupStore ?? throw new ArgumentNullException(nameof(groupStore));
            _roleStore = roleStore ?? throw new ArgumentNullException(nameof(roleStore));
            _userStore = userStore ?? throw new ArgumentNullException(nameof(userStore));
            _roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
        }

        public async Task<Group> AddGroup(Group group)
        {
            return await _groupStore.Add(group);
        }

        public async Task<Group> GetGroup(string id, string clientId)
        {
            var group = await _groupStore.Get(id);
            var clientRoles = await _roleService.GetRoles(clientId);
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

            var toDelete = allGroups.Where(g => !groupNames.Contains(g.Name, StringComparer.OrdinalIgnoreCase));
            var toAdd = groupList.Where(g => !storedGroupNames.Contains(g.Name, StringComparer.OrdinalIgnoreCase));

            // TODO: This must be transactional or fault tolerant.
            await Task.WhenAll(toDelete.ToList().Select(DeleteGroup));
            await Task.WhenAll(toAdd.ToList().Select(AddGroup));
        }

        public async Task<bool> Exists(string id)
        {
            return await _groupStore.Exists(id);
        }

        public async Task<Group> AddRoleToGroup(string groupName, Guid roleId)
        {
            var group = await _groupStore.Get(groupName);
            var role = await _roleStore.Get(roleId);

            if (group.Roles.All(r => r.Id != roleId))
            {
                group.Roles.Add(role);
            }

            if (role.Groups.All(g => g != groupName))
            {
                role.Groups.Add(groupName);
            }

            await _roleStore.Update(role);
            await _groupStore.Update(group);
            return group;
        }

        public async Task<Group> DeleteRoleFromGroup(string groupName, Guid roleId)
        {
            var group = await _groupStore.Get(groupName);
            var role = await _roleStore.Get(roleId);

            var groupRole = group.Roles.FirstOrDefault(r => r.Id == roleId);
            if (groupRole != null)
            {
                group.Roles.Remove(groupRole);
            }

            if (role.Groups.Any(g => g == groupName))
            {
                role.Groups.Remove(groupName);
            }

            await _roleStore.Update(role);
            await _groupStore.Update(group);
            return group;
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

            if (group.Users.All(u => u.SubjectId != subjectId && u.IdentityProvider != identityProvider))
            {
                group.Users.Add(user);
            }

            if (user.Groups.All(g => g != groupName))
            {
                user.Groups.Add(groupName);
            }

            await _userStore.Update(user);
            await _groupStore.Update(group);
            return group;
        }

        public async Task<Group> DeleteUserFromGroup(string groupName, string subjectId, string identityProvider)
        {
            var group = await _groupStore.Get(groupName);
            var user = await _userStore.Get($"{subjectId}:{identityProvider}");

            var groupUser = group.Users.FirstOrDefault(u => 
                u.Id == user.Id);

            if (groupUser != null)
            {
                group.Users.Remove(groupUser);
            }

            if (user.Groups.Any(g => g == groupName))
            {
                user.Groups.Remove(groupName);
            }

            await _userStore.Update(user);
            await _groupStore.Update(group);
            return group;
        }
    }
}