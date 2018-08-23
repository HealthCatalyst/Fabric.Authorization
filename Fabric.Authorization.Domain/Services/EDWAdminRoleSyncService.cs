namespace Fabric.Authorization.Domain.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Fabric.Authorization.Domain.Models;
    using Fabric.Authorization.Domain.Models.EDW;
    using Fabric.Authorization.Domain.Stores;

    public class EDWAdminRoleSyncService
    {
        private readonly RoleService _roleService;
        private readonly IEDWStore _edwStore;


        public EDWAdminRoleSyncService(RoleService roleService, IEDWStore edwStore)
        {
            this._roleService = roleService ??
                    throw new ArgumentNullException(nameof(roleService));
            this._edwStore = edwStore ??
                    throw new ArgumentNullException(nameof(edwStore));
        }

        public async Task RefreshDosAdminRolesAsync(User user)
        {
            if (await this.IsUserASuperAdmin(user))
            {
                _edwStore.AddIdentitiesToRole(new[] { user.SubjectId }, EDWConstants.EDWAdmin);
            }
            else
            {
                _edwStore.RemoveIdentitiesFromRole(new[] { user.SubjectId }, EDWConstants.EDWAdmin);
            }
        }

        public async Task RefreshDosAdminRolesAsync(IEnumerable<User> users)
        {
            foreach(User user in users)
            {
                await this.RefreshDosAdminRolesAsync(user);
            }
        }


        private async Task<bool> IsUserASuperAdmin(User user)
        {
            if (user.Roles.Any(r => RoleManagerConstants.AdminRoleNames.Contains(r.Name)))
            {
                return true;
            }

            // next, get all the groups for a user
            // if a group is a special Super Admin Group then return true
            // if not, then look on each group to see if there is a "super admin role"
            foreach (var customGroup in user.Groups)
            {
                // var group = await _groupService.GetGroup(customGroup.Name).ConfigureAwait(false);
                if (customGroup.Roles.Any(r => RoleManagerConstants.AdminRoleNames.Contains(r.Name)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
