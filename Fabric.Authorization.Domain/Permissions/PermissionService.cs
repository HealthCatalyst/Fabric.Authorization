using System;
using System.Collections.Generic;
using System.Linq;
using Fabric.Authorization.Domain.Exceptions;
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

        public Result<T> AddPermission<T>(string grain, string resource, string permissionName)
        {
            var newPermission = new Permission
            {
                Grain = grain,
                Resource = resource,
                Name = permissionName
            };

            var validationResults = _permissionValidator.Validate(newPermission);

            if (!validationResults.IsValid)
            {
                return new Result<T>
                {
                    ValidationResult = validationResults
                };
            }

            object addedPermission = _permissionStore.AddPermission(newPermission);
            return new Result<T>
            {
                Model = (T)addedPermission,
                ValidationResult = validationResults
            };
        }

        public void DeletePermission(Guid permissionId)
        {
            var permission = _permissionStore.GetPermission(permissionId);
            _permissionStore.DeletePermission(permission);
        }
    }

    public class Result<T>
    {
        public T Model { get; set; }
        public ValidationResult ValidationResult { get; set; } 
    }
}
