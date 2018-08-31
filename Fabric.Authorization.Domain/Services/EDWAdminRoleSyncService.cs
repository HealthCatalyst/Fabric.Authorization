namespace Fabric.Authorization.Domain.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Fabric.Authorization.Domain.Models;
    using Fabric.Authorization.Domain.Models.EDW;
    using Fabric.Authorization.Domain.Stores;

    public class DisableEDWAdminRoleSyncService : IEDWAdminRoleSyncService
    {
        public async Task RefreshDosAdminRolesAsync(User user)
        {
            return;
        }

        public async Task RefreshDosAdminRolesAsync(IEnumerable<User> users)
        {
            return;
        }
    }

    public class EDWAdminRoleSyncService : IEDWAdminRoleSyncService
    {
        private readonly IEDWStore _edwStore;


        public EDWAdminRoleSyncService(IEDWStore edwStore)
        {
            this._edwStore = edwStore ??
                    throw new ArgumentNullException(nameof(edwStore));
        }

        public async Task RefreshDosAdminRolesAsync(User user)
        {
            if (user == null)
            {
                return;
            }

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
            if (users == null)
            {
                return;
            }

            foreach (User user in users)
            {
                await this.RefreshDosAdminRolesAsync(user);
            }
        }

        private async Task<bool> IsUserASuperAdmin(User user)
        {
            if (user.IsDeleted)
            {
                return false;
            }

            if (user.Roles.Any(r => RoleManagerConstants.AdminRoleNames.Contains(r.Name.ToLower()) && !r.IsDeleted))
            {
                return true;
            }

            // next, get all the groups for a user
            // if a group is a special Super Admin Group then return true
            // if not, then look on each group to see if there is a "super admin role"
            // finally, look on each group's children groups to see if there is a "super admin role"
            foreach (var customGroup in user.Groups.Where(group => !group.IsDeleted))
            {
                if (customGroup.Roles.Any(r => RoleManagerConstants.AdminRoleNames.Contains(r.Name.ToLower()) && !r.IsDeleted))
                {
                    return true;
                }

                if (customGroup.Parents.Where(group => !group.IsDeleted).SelectMany(parent => parent.Roles)
                    .Any(role => RoleManagerConstants.AdminRoleNames.Contains(role.Name.ToLower()) && !role.IsDeleted))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
