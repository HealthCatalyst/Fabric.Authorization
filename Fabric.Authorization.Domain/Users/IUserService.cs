using System;
using System.Collections.Generic;

namespace Fabric.Authorization.Domain.Users
{
    public interface IUserService
    {
        IEnumerable<string> GetPermissionsForUser(string userId, string grain = null, string resource = null);
        IEnumerable<Role> GetRolesForUser(string userId, string grain = null, string resource = null);

        void AddRoleToUser(string userId, Guid roleId);

        void DeleteRoleFromUser(string userId, Guid roleId);
    }
}
