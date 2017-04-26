using System;
using System.Collections.Generic;
using System.Linq;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Roles;

namespace Fabric.Authorization.Domain
{
    public class UserService : IUserService
    {
        private readonly IUserStore _userStore;
        private readonly IRoleStore _roleStore;
        public UserService(IUserStore userStore, IRoleStore roleStore)
        {
            _userStore = userStore ?? throw new ArgumentNullException(nameof(userStore));
            _roleStore = roleStore ?? throw new ArgumentNullException(nameof(userStore));
        }

        public IEnumerable<string> GetPermissionsForUser(string userId, string grain = null, string resource = null)
        {
            var user = _userStore.GetUser(userId);
            if (user == null) throw new UserNotFoundException();

            var permissions = user.Permissions;
            if (!string.IsNullOrEmpty(grain))
            {
                permissions = permissions.Where(p => p.Grain == grain);
            }
            if (!string.IsNullOrEmpty(resource))
            {
                permissions = permissions.Where(p => p.Resource == resource);
            }
            return permissions.Select(p => p.ToString());
        }

        public IEnumerable<Role> GetRolesForUser(string userId, string grain = null, string resource = null)
        {
            var user = _userStore.GetUser(userId);
            if (user == null) throw new UserNotFoundException();

            var roles = user.Roles;
            if (!string.IsNullOrEmpty(grain))
            {
                roles = roles.Where(p => p.Grain == grain).ToList();
            }
            if (!string.IsNullOrEmpty(resource))
            {
                roles = roles.Where(p => p.Resource == resource).ToList();
            }
            return roles;
        }

        public void AddRoleToUser(string userId, int roleId, string grain, string resource, string roleName)
        {
            var user = _userStore.GetUser(userId);
            if (user == null) throw new UserNotFoundException();

            var role = _roleStore.GetRole(roleId);
            if (role == null) throw new RoleNotFoundException();

            if (user.Roles.All(r => r.Id != roleId))
            {
                user.Roles.Add(role);
            }
        }

        public void DeleteRoleFromUser(string userId, int roleId, string grain, string resource, string roleName)
        {
            var user = _userStore.GetUser(userId);
            if (user == null) throw new UserNotFoundException();

            var role = _roleStore.GetRole(roleId);
            if (role == null) throw new RoleNotFoundException();

            if (user.Roles.Any(r => r.Id == roleId))
            {
                user.Roles.Remove(role);
            }
        }
    }
}
