namespace Fabric.Authorization.Domain.Services
{
    using Fabric.Authorization.Domain.Models;
    using Fabric.Authorization.Domain.Stores;
    using System.Threading.Tasks;
    using System.Linq;

    public class RoleManager
    {
        private readonly RoleService _roleService;
        private readonly UserService _userService;
        private readonly GroupService _groupService;


        public RoleManager(RoleService roleService, UserService userService, GroupService groupService)
        {
            this._roleService = roleService;
            this._userService = userService;
            this._groupService = groupService;
        }
        
        public async Task<bool> IsUserASuperAdmin(string subjectId, string identityProvider, string roleGrain, string roleSecurableItem)
        {
            // Get all the roles for a given user and see if they are a "super admin"
            var roles = await _userService.GetRolesForUser(subjectId, identityProvider);
            if (roles.Any(r => RoleManagerConstants.AdminRoleNames.Contains(r.Name)))
            {
                return true;
            }

            // next, get all the groups for a user
            // if a group is a special Super Admin Group then return true
            // if not, then look on each group to see if there is a "super admin role"
            var identityGroups = await _userService.GetGroupsForUser(subjectId, identityProvider);
            foreach (var customGroup in identityGroups)
            {
                var group = await _groupService.GetGroup(customGroup.Name).ConfigureAwait(false);
                var groupRoles = group.Roles.Where(p => p.SecurableItem == roleSecurableItem && p.Grain == roleGrain);
                if (groupRoles.Any(r => RoleManagerConstants.AdminRoleNames.Contains(r.Name)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
