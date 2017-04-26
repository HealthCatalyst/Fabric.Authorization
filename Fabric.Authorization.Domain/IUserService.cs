using System.Collections.Generic;

namespace Fabric.Authorization.Domain
{
    public interface IUserService
    {
        IEnumerable<string> GetPermissionsForUser(string userId, string grain = null, string resource = null);
        IEnumerable<string> GetRolesForUser(string userId, string grain = null, string resource = null);
    }
}
