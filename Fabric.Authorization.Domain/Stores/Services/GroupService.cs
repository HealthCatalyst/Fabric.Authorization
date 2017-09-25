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

        public GroupService(
            IGroupStore groupStore,
            IRoleStore roleStore,
            IUserStore userStore)
        {
            _groupStore = groupStore ?? throw new ArgumentNullException(nameof(groupStore));
            _roleStore = roleStore ?? throw new ArgumentNullException(nameof(roleStore));
            _userStore = userStore ?? throw new ArgumentNullException(nameof(userStore));
        }

        public async Task<Group> AddGroup(Group group)
        {
            return await _groupStore.Add(group);
        }

        public async Task<Group> GetGroup(string id)
        {
            return await _groupStore.Get(id);
        }

        public async Task<IEnumerable<Group>> GetAllGroups()
        {
            return await _groupStore.GetAll();
        }

        public async Task DeleteGroup(Group group)
        {
            await _groupStore.Delete(group);
        }

        public async Task UpdateGroupList(IEnumerable<Group> groups)
        {
            var allGroups = await _groupStore.GetAll() ?? Enumerable.Empty<Group>();

            var groupNames = groups.Select(g => g.Name);
            var storedGroupNames = allGroups.Select(g => g.Name);

            var toDelete = allGroups.Where(g => !groupNames.Contains(g.Name));
            var toAdd = groups.Where(g => !storedGroupNames.Contains(g.Name));

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

            if (!string.Equals(group.Source, GroupConstants.CustomSource))
            {
                throw new BadRequestException<Group>();
            }

            User user;
            try
            {
                user = await _userStore.Get(subjectId);
            }
            catch (NotFoundException<User>)
            {
                user = await _userStore.Add(new User {SubjectId = subjectId, IdentityProvider = identityProvider});
            }

            if (group.Users.All(u => u.SubjectId != subjectId))
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

        public async Task<Group> DeleteUserFromGroup(string groupName, string subjectId)
        {
            var group = await _groupStore.Get(groupName);
            var user = await _userStore.Get(subjectId);

            var groupUser = group.Users.FirstOrDefault(u => u.SubjectId == subjectId);
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