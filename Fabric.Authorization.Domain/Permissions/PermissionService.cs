using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fabric.Authorization.Domain.Exceptions;

namespace Fabric.Authorization.Domain.Permissions
{
    public class PermissionService : IPermissionService
    {
        private readonly IPermissionStore _permissionStore;

        public PermissionService(IPermissionStore permissionStore)
        {
            _permissionStore = permissionStore ?? throw new ArgumentNullException(nameof(permissionStore));
        }
        public IEnumerable<Permission> GetPermissions(string grain = null, string resource = null, string permissionName = null)
        {
            return _permissionStore.GetPermissions(grain, resource, permissionName);
        }

        public Permission GetPermission(Guid permissionId)
        {
            return _permissionStore.GetPermission(permissionId);
        }

        public Permission AddPermission(string grain, string resource, string permissionName)
        {
            if (_permissionStore.GetPermissions(grain, resource, permissionName).Any())
            {
                throw new PermissionAlreadyExistsException();
            }
            return _permissionStore.AddPermission(new Permission
            {
                Grain = grain,
                Resource = resource,
                Name = permissionName
            });
        }

        public void DeletePermission(Guid permissionId)
        {
            var permission = _permissionStore.GetPermission(permissionId);
            _permissionStore.DeletePermission(permission);
        }
    }
}
