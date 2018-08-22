using Catalyst.Security.Services;
using Fabric.Authorization.API.Models.EDW;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Authorization.API.Services
{
    public class RoleManager
    {
        private readonly ISecurityContext securityContext;

        public RoleManager(ISecurityContext securityContext)
        {
            this.securityContext = securityContext;
        }

        public IQueryable<EDWRole> GetRolesByName(string roleName)
        {
            return this.securityContext.EDWRoles.Where(role => role.Name == roleName);
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

        public void AddIdentitiesToRole(string[] identityNames, int roleId)
        {
            using (var context = this.securityContext.CreateContext())
            {
                foreach (string identityName in identityNames)
                {
                    if (!context.EDWIdentities.Any(identity => identity.Name == identityName))
                    {
                        context.EDWIdentities.Add(new EDWIdentity { Name = identityName });
                    }
                }

                context.SaveChanges();

                var roleToAdd = context.EDWRoles.FirstOrDefault(role => role.Id == roleId);

                if(roleToAdd == null)
                {
                    throw new ArgumentException("Role not found.");
                }

                var targetIdentities = context.EDWIdentities.Include(identity => identity.EDWRoles)
                    .Where(identity => identityNames.Contains(identity.Name) && !identity.EDWRoles.Any(role => role.Id == roleId))
                    .ToList();

                foreach (var identity in targetIdentities)
                {
                    roleToAdd.EDWIdentities.Add(identity);
                }

                context.SaveChanges();
            }
        }

        public void RemoveIdentitiesFromRole(string[] identityNames, int roleId)
        {
            using (var context = this.securityContext.CreateContext())
            {
                var targetIdentities = securityContext.EDWIdentities.Include(identity => identity.EDWRoles)
                .Where(identity => identityNames.Contains(identity.Name));

                foreach (var identity in targetIdentities)
                {
                    var roleToRemove = identity.EDWRoles.FirstOrDefault(role => role.Id == roleId);
                    if (roleToRemove == null)
                    {
                        continue;
                    }

                    identity.EDWRoles.Remove(roleToRemove);
                }

                securityContext.SaveChanges();
            }
        }
    }
}
