using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;

namespace Fabric.Authorization.Domain.Services
{
    public class GroupService
    {
        private readonly IGroupStore _groupStore;
        private readonly IRoleStore _roleStore;

        public GroupService(IGroupStore groupStore, IRoleStore roleStore)
        {
            _groupStore = groupStore ?? throw new ArgumentNullException(nameof(groupStore));
            _roleStore = roleStore ?? throw new ArgumentNullException(nameof(roleStore));
        }

        public async Task<IEnumerable<string>> GetPermissionsForGroups(string[] groupNames, string grain = null, string securableItem = null)
        {
            var permissions = new List<string>();
            foreach (var groupName in groupNames)
            {
                var baseRoles = await this.GetRolesForGroup(groupName, grain, securableItem).ConfigureAwait(false);
                var roles = new Dictionary<Guid, Role>();
                foreach (var role in baseRoles)
                {
                    if (!role.ParentRole.HasValue)
                    {
                        continue;
                    }
                    var hierarchy = await _roleStore.GetRoleHierarchy(role.Id).ConfigureAwait(false);
                    foreach (var ancestorRole in hierarchy)
                    {
                        if (!roles.ContainsKey(ancestorRole.Id))
                        {
                            roles.Add(ancestorRole.Id, ancestorRole);
                        }
                    }
                }

                foreach (var baseRole in baseRoles)
                {
                    if (!roles.ContainsKey(baseRole.Id))
                    {
                        roles.Add(baseRole.Id, baseRole);
                    }
                }
                if (roles.Any())
                {
                    permissions
                    .AddRange(roles.Values
                        .Where(r => r.Permissions != null && !r.IsDeleted)
                        .SelectMany(r => r.Permissions.Where(p => 
                            !p.IsDeleted && 
                            (p.Grain == grain || grain == null) &&
                            (p.SecurableItem == securableItem || securableItem == null))
                        .Select(p => p.ToString())));
                }
            }

            return permissions.Distinct();
        }

        public async Task<IEnumerable<Role>> GetRolesForGroup(string groupName, string grain = null, string securableItem = null)
        {
            if (! await _groupStore.Exists(groupName))
            {
                return new List<Role>();
            }

            var group = await _groupStore.Get(groupName);

            var roles = group.Roles;
            if (!string.IsNullOrEmpty(grain))
            {
                roles = roles.Where(p => p.Grain == grain).ToList();
            }
            if (!string.IsNullOrEmpty(securableItem))
            {
                roles = roles.Where(p => p.SecurableItem == securableItem).ToList();
            }

            return roles.Where(r => !r.IsDeleted);
        }

        public async Task AddRoleToGroup(string groupName, Guid roleId)
        {
            var group = await _groupStore.Get(groupName);
            var role = await _roleStore.Get(roleId);

            if (group.Roles.All(r => r.Id != roleId))
            {
                group.Roles.Add(role);
            }

            await _groupStore.Update(group);
        }

        public async Task DeleteRoleFromGroup(string groupName, Guid roleId)
        {
            var group = await _groupStore.Get(groupName);
            var role = await _roleStore.Get(roleId);

            if (group.Roles.Any(r => r.Id == roleId))
            {
                group.Roles.Remove(role);
            }

            await _groupStore.Update(group);
        }

        public async Task<Group> AddGroup(Group group) => await _groupStore.Add(group);

        public async Task<Group> GetGroup(string id) => await _groupStore.Get(id);

        public async Task DeleteGroup(Group group) => await _groupStore.Delete(group);

        public async Task UpdateGroupList(IEnumerable<Group> groups)
        {
            var allGroups = await _groupStore.GetAll() ?? Enumerable.Empty<Group>();

            var groupNames = groups.Select(g => g.Name);
            var storedGroupNames = allGroups.Select(g => g.Name);

            var toDelete = allGroups.Where(g => !groupNames.Contains(g.Name));
            var toAdd = groups.Where(g => !storedGroupNames.Contains(g.Name));

            // TODO: This must be transactional or fault tolerant.
            await Task.WhenAll(toDelete.ToList().Select(this.DeleteGroup));
            await Task.WhenAll(toAdd.ToList().Select(this.AddGroup));
        }
    }
}