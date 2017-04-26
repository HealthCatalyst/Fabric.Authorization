using System;
using System.Collections.Generic;
using System.Linq;
using Fabric.Authorization.Domain.Exceptions;

namespace Fabric.Authorization.Domain
{
    public class PermissionService : IPermissionService
    {
        private readonly IUserStore _userStore;
        public PermissionService(IUserStore userStore)
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
    }
}
