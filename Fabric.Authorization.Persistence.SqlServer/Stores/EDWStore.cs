using System;
using System.Linq;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Persistence.SqlServer.EntityModels.EDW;
using Fabric.Authorization.Persistence.SqlServer.Stores.EDW;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Authorization.Persistence.SqlServer.Stores
{
    public class EDWStore : /*SqlServerBaseStore,TODO EDWServerBaseStore?*/ IEDWStore
    {
        private readonly ISecurityContext securityContext;

        public EDWStore(ISecurityContext securityContext)
        {
            this.securityContext = securityContext ??
                    throw new ArgumentNullException(nameof(securityContext));
        }

        public void AddIdentitiesToRole(string[] identities, string roleName)
        {
            var role = this.GetRolesByName(roleName).SingleOrDefault();
            this.AddIdentitiesToRole(identities, role.Id);
        }


        public void RemoveIdentitiesFromRole(string[] identities, string roleName)
        {
            var role = this.GetRolesByName(roleName).SingleOrDefault();
            this.RemoveIdentitiesFromRole(identities, role.Id);
        }


        private IQueryable<EDWRole> GetRolesByName(string roleName)
        {
            return this.securityContext.EDWRoles.Where(role => role.Name == roleName);
        }

        private void AddIdentitiesToRole(string[] identityNames, int roleId)
        {
            foreach (string identityName in identityNames)
            {
                if (!securityContext.EDWIdentities.Any(identity => identity.Name == identityName))
                {
                    securityContext.EDWIdentities.Add(new EDWIdentity { Name = identityName });
                }
            }

            securityContext.SaveChanges();

            var roleToAdd = securityContext.EDWRoles.FirstOrDefault(role => role.Id == roleId);

            if (roleToAdd == null)
            {
                throw new ArgumentException("Role not found.");
            }

            var targetIdentities = securityContext.EDWIdentities.Include(identity => identity.EDWIdentityRoles)
                .Where(identity => identityNames.Contains(identity.Name) && !identity.EDWRoles.Any(role => role.Id == roleId))
                .ToList();

            foreach (var identity in targetIdentities)
            {
                if(identity.EDWIdentityRoles.Where(ir => ir.RoleID == roleId).Any())
                {
                    continue;
                }

                EDWIdentityRole identityRoleMappingToAdd = new EDWIdentityRole
                {
                    RoleID = roleId,
                    IdentityID = identity.Id
                };
                roleToAdd.EDWIdentityRoles.Add(identityRoleMappingToAdd);
            }

            securityContext.SaveChanges();
        }

        private void RemoveIdentitiesFromRole(string[] identityNames, int roleId)
        {
            var targetIdentities = securityContext.EDWIdentities.Include(identity => identity.EDWIdentityRoles)
            .Where(identity => identityNames.Contains(identity.Name));

            foreach (var identity in targetIdentities)
            {
                var identityRoleMappingToRemove = identity.EDWIdentityRoles.Where(ir => ir.RoleID == roleId).FirstOrDefault();
                if (identityRoleMappingToRemove == null)
                {
                    continue;
                }

                identity.EDWIdentityRoles.Remove(identityRoleMappingToRemove);
            }

            securityContext.SaveChanges();
        }
    }
}
