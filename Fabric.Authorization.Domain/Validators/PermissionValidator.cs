using System;
using System.Linq;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;
using FluentValidation;

namespace Fabric.Authorization.Domain.Validators
{
    public class PermissionValidator : AbstractValidator<Permission>
    {
        private readonly IPermissionStore _permissionStore;
        public PermissionValidator(IPermissionStore permissionStore)
        {
            _permissionStore = permissionStore ?? throw new ArgumentNullException(nameof(permissionStore));
            ConfigureRules();
        }

        private void ConfigureRules()
        {
            RuleFor(permission => permission.Grain)
                .NotEmpty()
                .WithMessage("Please specify a Grain for this permission");

            RuleFor(permission => permission.SecurableItem)
                .NotEmpty()
                .WithMessage("Please specify a SecurableItem for this permission");

            RuleFor(permission => permission.Name)
                .NotEmpty()
                .WithMessage("Please specify a Name for this permission");

            RuleFor(permission => permission)
                .Must(BeUnique)
                .When(permission => !string.IsNullOrEmpty(permission.Grain)
                                    && !string.IsNullOrEmpty(permission.SecurableItem)
                                    && !string.IsNullOrEmpty(permission.Name))
                .WithMessage("The permission already exists");
        }

        public bool BeUnique(Permission permission)
        {
            return !_permissionStore
                    .GetPermissions(permission.Grain, permission.SecurableItem, permission.Name)
                    .Any();
        }
    }
}
