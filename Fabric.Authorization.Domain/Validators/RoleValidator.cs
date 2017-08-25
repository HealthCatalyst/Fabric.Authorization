using System;
using System.Linq;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores.Services;
using FluentValidation;

namespace Fabric.Authorization.Domain.Validators
{
    public class RoleValidator : AbstractValidator<Role>
    {
        private readonly RoleService _roleService;

        public RoleValidator(RoleService roleService)
        {
            _roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
            ConfigureRules();
        }

        private void ConfigureRules()
        {
            RuleFor(role => role.Grain)
                .NotEmpty()
                .WithMessage("Please specify a Grain for this role");

            RuleFor(role => role.SecurableItem)
                .NotEmpty()
                .WithMessage("Please specify a SecurableItem for this role");

            RuleFor(role => role.Name)
                .NotEmpty()
                .WithMessage("Please specify a Name for this role");

            RuleFor(role => role)
                .Must(BeUnique)
                .When(role => !string.IsNullOrEmpty(role.Grain)
                              && !string.IsNullOrEmpty(role.SecurableItem)
                              && !string.IsNullOrEmpty(role.Name))
                .WithMessage("The role already exists");
        }

        private bool BeUnique(Role role)
        {
            return !_roleService
                .GetRoles(role.Grain, role.SecurableItem, role.Name)
                .Result
                .Any();
        }
    }
}