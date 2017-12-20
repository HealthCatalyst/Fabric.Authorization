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
        private readonly RoleService _roleService;

        public GroupService(
            IGroupStore groupStore,
            RoleService roleService)
        {
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
            return await _groupStore.AddRoleToGroup(groupName, roleId);
        }

        public async Task<Group> DeleteRoleFromGroup(string groupName, Guid roleId)
        {
            return await _groupStore.DeleteRoleFromGroup(groupName, roleId);
        }

        public async Task<Group> AddUserToGroup(string groupName, string subjectId, string identityProvider)
        {
            return await _groupStore.AddUserToGroup(groupName, subjectId, identityProvider);
        }

        public async Task<Group> DeleteUserFromGroup(string groupName, string subjectId, string identityProvider)
        {
           return await _groupStore.DeleteUserFromGroup(groupName, subjectId, identityProvider);
        }
    }
}