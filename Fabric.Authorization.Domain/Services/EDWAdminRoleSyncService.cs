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

            if (IsUserASuperAdmin(user))
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

        /// <summary>
        /// Check to see if the user is in the DosAdmin group.
        /// </summary>
        /// <param name="user">The user to be checked</param>
        /// <returns>true or false</returns>
        private static bool IsUserASuperAdmin(User user)
        {
            return !user.IsDeleted 
                   && user.Groups.Any(group => !group.IsDeleted
                                               && group.NameEquals(RoleManagerConstants.DosAdminGroupName));
        }
    }
}
