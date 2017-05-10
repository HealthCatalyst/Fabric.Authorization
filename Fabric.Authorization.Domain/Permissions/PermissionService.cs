using System;
using System.Collections.Generic;
using Fabric.Authorization.Domain.Validators;
using FluentValidation.Results;

namespace Fabric.Authorization.Domain.Permissions
{
    public class PermissionService : IPermissionService
    {
        private readonly IPermissionStore _permissionStore;
        private readonly PermissionValidator _permissionValidator;

        public PermissionService(IPermissionStore permissionStore, PermissionValidator permissionValidator)
        {
            _permissionStore = permissionStore ?? throw new ArgumentNullException(nameof(permissionStore));
            _permissionValidator = permissionValidator ?? throw new ArgumentNullException(nameof(permissionValidator));
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
            var newPermission = CreatePermission(grain, resource, permissionName);

            return _permissionStore.AddPermission(newPermission);
        }

        public Result<Permission> ValidatePermission(string grain, string resource, string permissionName)
        {
            var newPermission = CreatePermission(grain, resource, permissionName);

            var validationResults = _permissionValidator.Validate(newPermission);
            return new Result<Permission> { ValidationResult = validationResults, Model = newPermission };
        }

        public void DeletePermission(Permission permission)
        {
            _permissionStore.DeletePermission(permission);
        }

        private Permission CreatePermission(string grain, string resource, string permissionName)
        {
            return new Permission
            {
                Grain = grain,
                Resource = resource,
                Name = permissionName
            };
        }
    }

    public class Result<T>
    {
        public T Model { get; set; }
        public ValidationResult ValidationResult { get; set; } 
    }
}
