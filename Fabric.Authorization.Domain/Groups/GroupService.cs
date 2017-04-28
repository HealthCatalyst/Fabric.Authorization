using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Roles;

namespace Fabric.Authorization.Domain.Groups
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

        public IEnumerable<string> GetPermissionsForGroups(string[] groupNames, string grain = null, string resource = null)
        {
            var permissions = new List<string>();
            foreach (var groupName in groupNames)
            {
                var roles = GetRolesForGroup(groupName, grain, resource);
                permissions
                    .AddRange(roles
                        .Where(r => r.Permissions != null && !r.IsDeleted)
                        .SelectMany(r => r.Permissions.Where(p => !p.IsDeleted && (p.Grain == grain || grain == null) 
                                                        && (p.Resource == resource || resource == null))
                        .Select(p => p.ToString())));
            }
            return permissions;
        }

        public IEnumerable<Role> GetRolesForGroup(string groupName, string grain = null, string resource = null)
        {
            var group = _groupStore.GetGroup(groupName);
            if (group == null) throw new GroupNotFoundException();

            var roles = group.Roles;
            if (roles == null) return new List<Role>();
            if (!string.IsNullOrEmpty(grain))
            {
                roles = roles.Where(p => p.Grain == grain).ToList();
            }
            if (!string.IsNullOrEmpty(resource))
            {
                roles = roles.Where(p => p.Resource == resource).ToList();
            }
            return roles.Where(r => !r.IsDeleted);
        }

        public void AddRoleToGroup(string groupName, Guid roleId)
        {
            var group = _groupStore.GetGroup(groupName);
            if (group == null) throw new GroupNotFoundException();

            var role = _roleStore.GetRole(roleId);
            if (role == null) throw new RoleNotFoundException();

            if (group.Roles.All(r => r.Id != roleId))
            {
                group.Roles.Add(role);
            }
        }

        public void DeleteRoleFromGroup(string groupName, Guid roleId)
        {
            var group = _groupStore.GetGroup(groupName);
            if (group == null) throw new UserNotFoundException();

            var role = _roleStore.GetRole(roleId);
            if (role == null) throw new RoleNotFoundException();

            if (group.Roles.Any(r => r.Id == roleId))
            {
                group.Roles.Remove(role);
            }
        }
    }
}
