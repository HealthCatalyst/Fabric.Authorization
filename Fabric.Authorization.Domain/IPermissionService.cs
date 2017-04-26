using System.Collections.Generic;

namespace Fabric.Authorization.Domain
{
    public interface IPermissionService
    {
        IEnumerable<string> GetPermissionsForUser(string userId, string grain = null, string resource = null);
    }
}
