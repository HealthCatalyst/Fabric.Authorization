using System;
using System.Linq;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using FluentValidation;
using FluentValidation.Validators;

namespace Fabric.Authorization.Domain.Validators
{
    public class GroupValidator : AbstractValidator<Group>
    {
        public GroupValidator()
        {
            ConfigureRules();
        }

        private void ConfigureRules()
        {
            RuleFor(group => group.Name)
                .NotEmpty()
                .WithMessage("Please specify a Name for this Group.")
                .WithState(g => ValidationEnums.ValidationState.MissingRequiredField);

            RuleFor(group => group.Source)
                .NotEmpty()
                .WithMessage("Please specify a Source for this Group.")
                .WithState(g => ValidationEnums.ValidationState.MissingRequiredField);

            RuleFor(group => group).Custom(ValidateIdentityProvider);
        }

        private static void ValidateIdentityProvider(Group group, CustomContext context)
        {
            if (group.SourceEquals(GroupConstants.CustomSource))
            {
                if (!string.IsNullOrWhiteSpace(group.IdentityProvider))
                {
                    context.AddFailure("Custom groups are not allowed to have an IdentityProvider.");
                }

                return;
            }

            if (group.SourceEquals(GroupConstants.DirectorySource))
            {
                if (string.IsNullOrWhiteSpace(group.IdentityProvider))
                {
                    context.AddFailure("Please specify an IdentityProvider for this Group.");
                }

                if (!IdentityConstants.ValidIdentityProviders.Contains(group.IdentityProvider,
                    StringComparer.OrdinalIgnoreCase))
                {
                    context.AddFailure($"Please specify a valid IdentityProvider. Valid identity providers include the following: {string.Join(", ", IdentityConstants.ValidIdentityProviders)}");
                }
            }
        }
    }
}