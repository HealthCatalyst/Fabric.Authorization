using System;
using System.Collections.Generic;

namespace Fabric.Authorization.Domain.Services
{
    public interface IGroupService
    {
        IEnumerable<string> GetPermissionsForGroups(string[] groupNames, string grain = null, string resource = null);

        IEnumerable<Role> GetRolesForGroup(string groupName, string grain = null, string resource = null);

        void AddRoleToGroup(string groupName, Guid roledId);

        void DeleteRoleFromGroup(string groupName, Guid roleId);
    }
}
