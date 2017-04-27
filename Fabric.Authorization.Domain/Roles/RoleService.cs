using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fabric.Authorization.Domain.Exceptions;

namespace Fabric.Authorization.Domain.Roles
{
    public class RoleService : IRoleService
    {
        private readonly IRoleStore _roleStore;
        public RoleService(IRoleStore roleStore)
        {
            _roleStore = roleStore ?? throw new ArgumentNullException(nameof(roleStore));
        }
        public IEnumerable<Role> GetRoles(string grain = null, string resource = null, string roleName = null)
        {
            return _roleStore.GetRoles(grain, resource, roleName);
        }

        public IEnumerable<Permission> GetPermissionsForRole(int roleId)
        {
            throw new NotImplementedException();
        }

        public void AddRole(string grain, string resource, string roleName)
        {
            if (_roleStore.GetRoles(grain, resource, roleName).Any())
            {
                throw new RoleAlreadyExistsException();
            }
            _roleStore.AddRole(new Role
            {
                Grain = grain,
                Resource = resource,
                Name = roleName
            });
        }

        public void DeleteRole(Guid roleId)
        {
            var role = _roleStore.GetRole(roleId);
            if (role == null)
            {
                throw new RoleNotFoundException(); 
            }
            _roleStore.DeleteRole(role);
        }

        public void AddPermissionsToRole(Guid[] permissionIds)
        {
            throw new NotImplementedException();
        }

        public void RemovePermissionsFromRole(Guid[] permissionIds)
        {
            throw new NotImplementedException();
        }
    }
}
