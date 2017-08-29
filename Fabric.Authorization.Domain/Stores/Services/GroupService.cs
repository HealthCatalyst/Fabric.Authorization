using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores.Services
{
    public class GroupService
    {
        private readonly IGroupStore _groupStore;
        private readonly IRoleStore _roleStore;
        private readonly IUserStore _userStore;

        private readonly string[] _customGroupSources = {"Custom"};

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

        public async Task<IEnumerable<Role>> GetRolesForGroup(string groupName, string grain = null,
            string securableItem = null)
        {
            if (!await _groupStore.Exists(groupName))
                return new List<Role>();

            var group = await _groupStore.Get(groupName);

            var roles = group.Roles;
            if (!string.IsNullOrEmpty(grain))
                roles = roles.Where(p => p.Grain == grain).ToList();
            if (!string.IsNullOrEmpty(securableItem))
                roles = roles.Where(p => p.SecurableItem == securableItem).ToList();

            return roles.Where(r => !r.IsDeleted);
        }

        public async Task<Group> AddRoleToGroup(string groupName, Guid roleId)
        {
            var group = await _groupStore.Get(groupName);
            var role = await _roleStore.Get(roleId);

            if (group.Roles.All(r => r.Id != roleId))
                group.Roles.Add(role);

            if (role.Groups.All(g => g != groupName))
                role.Groups.Add(groupName);

            await _roleStore.Update(role);
            await _groupStore.Update(group);
            return group;
        }

        public async Task<Group> DeleteRoleFromGroup(string groupName, Guid roleId)
        {
            var group = await _groupStore.Get(groupName);
            var role = await _roleStore.Get(roleId);

            if (group.Roles.Any(r => r.Id == roleId))
                group.Roles.Remove(role);

            if (role.Groups.Any(g => g == groupName))
                role.Groups.Remove(groupName);

            await _roleStore.Update(role);
            await _groupStore.Update(group);
            return group;
        }

        public async Task<IEnumerable<User>> GetUsersForGroup(string groupName)
        {
            if (!await _groupStore.Exists(groupName))
                return new List<User>();

            var group = await _groupStore.Get(groupName);
            var users = group.Users;
            return users.Where(u => !u.IsDeleted);
        }

        public async Task<Group> AddUserToGroup(string groupName, string subjectId)
        {
            var group = await _groupStore.Get(groupName);

            if (!_customGroupSources.Contains(group.Source))
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
                user = await _userStore.Add(new User {SubjectId = subjectId});
            }

            if (group.Users.All(u => u.SubjectId != subjectId))
                group.Users.Add(user);

            if (user.Groups.All(g => g != groupName))
                user.Groups.Add(groupName);

            await _userStore.Update(user);
            await _groupStore.Update(group);
            return group;
        }

        public async Task<Group> DeleteUserFromGroup(string groupName, string subjectId)
        {
            var group = await _groupStore.Get(groupName);
            var user = await _userStore.Get(subjectId);

            if (group.Users.Any(r => r.SubjectId == subjectId))
                group.Users.Remove(user);

            if (user.Groups.Any(g => g == groupName))
                user.Groups.Remove(groupName);

            await _userStore.Update(user);
            await _groupStore.Update(group);
            return group;
        }
    }
}