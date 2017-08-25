﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores.Services
{
    public class GroupService
    {
        private readonly IGroupStore _groupStore;
        private readonly IRoleStore _roleStore;

        public GroupService(IGroupStore groupStore, IRoleStore roleStore, RoleService roleService)
        {
            _groupStore = groupStore ?? throw new ArgumentNullException(nameof(groupStore));
            _roleStore = roleStore ?? throw new ArgumentNullException(nameof(roleStore));
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
    }
}