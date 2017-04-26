using System.Collections.Generic;

namespace Fabric.Authorization.Domain
{
    public interface IUserService
    {
        IEnumerable<string> GetPermissionsForUser(string userId, string grain = null, string resource = null);
        IEnumerable<Role> GetRolesForUser(string userId, string grain = null, string resource = null);

        void AddRoleToUser(string userId, int roleId, string grain, string resource, string roleName);

        void DeleteRoleFromUser(string userId, int roleId, string grain, string resource, string roleName);
    }
}
