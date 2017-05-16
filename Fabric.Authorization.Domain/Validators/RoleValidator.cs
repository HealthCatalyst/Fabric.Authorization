using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fabric.Authorization.Domain.Validators
{
    public class RoleValidator : AbstractValidator<Role>
    {
        private readonly IRoleStore _roleStore;
        public RoleValidator(IRoleStore roleStore)
        {
            _roleStore = roleStore ?? throw new ArgumentNullException(nameof(roleStore));
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
            return !_roleStore
                    .GetRoles(role.Grain, role.SecurableItem, role.Name)
                    .Any();
        }
    }
}
