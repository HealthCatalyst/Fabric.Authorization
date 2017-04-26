using System;
using System.Collections.Generic;
using System.Linq;
using Fabric.Authorization.Domain.Exceptions;

namespace Fabric.Authorization.Domain
{
    public class UserService : IUserService
    {
        private readonly IUserStore _userStore;
        public UserService(IUserStore userStore)
        {
            _userStore = userStore ?? throw new ArgumentNullException(nameof(userStore));
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
                roles = roles.Where(p => p.Grain == grain);
            }
            if (!string.IsNullOrEmpty(resource))
            {
                roles = roles.Where(p => p.Resource == resource);
            }
            return roles;
        }

        public void AddRoleToUser(string userId, string grain, string resource, string roleName)
        {
            //first check if user exists
            //then check if role exists
            //if all is ok, add the role to the user.
            throw new NotImplementedException();
        }

        public void DeleteRoleFromUser(string userId, string grain, string resource, string roleName)
        {
            //first check if user exists
            //check if user has indicated role
            //if all is ok then remove the role from the user
            throw new NotImplementedException();
        }
    }
}
