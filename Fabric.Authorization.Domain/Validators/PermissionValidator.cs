using System;
using System.Linq;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores.Services;
using FluentValidation;

namespace Fabric.Authorization.Domain.Validators
{
    public class PermissionValidator : AbstractValidator<Permission>
    {
        private readonly PermissionService _permissionService;

        public PermissionValidator(PermissionService permissionService)
        {
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));            
            ConfigureRules();
        }

        private void ConfigureRules()
        {
            RuleFor(permission => permission.Grain)
                .NotEmpty()
                .WithMessage("Please specify a Grain for this permission")
                .WithState(p => ValidationEnums.ValidationState.MissingRequiredField);

            RuleFor(permission => permission.SecurableItem)
                .NotEmpty()
                .WithMessage("Please specify a SecurableItem for this permission")
                .WithState(p => ValidationEnums.ValidationState.MissingRequiredField);

            RuleFor(permission => permission.Name)
                .NotEmpty()
                .WithMessage("Please specify a Name for this permission")
                .WithState(p => ValidationEnums.ValidationState.MissingRequiredField);

            RuleFor(permission => permission)
                .Must(BeUnique)
                .When(permission => !string.IsNullOrEmpty(permission.Grain)
                                    && !string.IsNullOrEmpty(permission.SecurableItem)
                                    && !string.IsNullOrEmpty(permission.Name))
                .WithMessage(p => $"Permission {p.Name} already exists. Please provide a new name")
                .WithState(p => ValidationEnums.ValidationState.Duplicate);
        }

        private bool BeUnique(Permission permission)
        {
            return !_permissionService
                    .GetPermissions(permission.Grain, permission.SecurableItem, permission.Name)
                    .Result
                    .Any();
        }
    }
}