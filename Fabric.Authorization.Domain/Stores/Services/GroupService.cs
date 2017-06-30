using System;
using System.Collections.Generic;
using System.Linq;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;

namespace Fabric.Authorization.Domain.Services
{
    public class GroupService : IGroupService
    {
        private readonly IGroupStore _groupStore;
        private readonly IRoleStore _roleStore;

        public GroupService(IGroupStore groupStore, IRoleStore roleStore)
        {
            _groupStore = groupStore ?? throw new ArgumentNullException(nameof(groupStore));
            _roleStore = roleStore ?? throw new ArgumentNullException(nameof(roleStore));
        }

        public IEnumerable<string> GetPermissionsForGroups(string[] groupNames, string grain = null, string securableItem = null)
        {
            var permissions = new List<string>();
            foreach (var groupName in groupNames)
            {
                var roles = GetRolesForGroup(groupName, grain, securableItem);
                if (roles.Any())
                {
                    permissions
                    .AddRange(roles
                        .Where(r => r.Permissions != null && !r.IsDeleted)
                        .SelectMany(r => r.Permissions.Where(p => !p.IsDeleted && (p.Grain == grain || grain == null)
                                                        && (p.SecurableItem == securableItem || securableItem == null))
                        .Select(p => p.ToString())));
                }
            }
            return permissions;
        }

        public IEnumerable<Role> GetRolesForGroup(string groupName, string grain = null, string securableItem = null)
        {
            if (!_groupStore.Exists(groupName))
            {
                return new List<Role>();
            }

            var group = _groupStore.Get(groupName);

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

        public void AddRoleToGroup(string groupName, Guid roleId)
        {
            var group = _groupStore.Get(groupName);
            var role = _roleStore.Get(roleId);

            if (group.Roles.All(r => r.Id != roleId))
            {
                group.Roles.Add(role);
            }
        }

        public void DeleteRoleFromGroup(string groupName, Guid roleId)
        {
            var group = _groupStore.Get(groupName);
            var role = _roleStore.Get(roleId);

            if (group.Roles.Any(r => r.Id == roleId))
            {
                group.Roles.Remove(role);
            }
        }

        public void AddGroup(Group group) =>_groupStore.Add(group);

        public Group GetGroup(string id) =>  _groupStore.Get(id);

        public void DeleteGroup(Group group) => _groupStore.Delete(group);

        public void UpdateGroupList(IEnumerable<Group> groups)
        {
            var allGroups = _groupStore.GetAll();

            var groupNames = groups.Select(g => g.Name);
            var storedGroupNames = allGroups.Select(g => g.Name);

            var toDelete = allGroups.Where(g => !groupNames.Contains(g.Name));
            var toAdd = groups.Where(g => !storedGroupNames.Contains(g.Name));

            // TODO: This must be transactional or fault tolerant.
            toDelete.ToList().ForEach(this.DeleteGroup);
            toAdd.ToList().ForEach(this.AddGroup);
        }
    }
}
