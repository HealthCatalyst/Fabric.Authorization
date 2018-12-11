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

            RuleFor(group => group)
                .Must(g => g.SourceEquals(GroupConstants.CustomSource) && string.IsNullOrWhiteSpace(g.IdentityProvider))
                .WithMessage("Custom groups are not allowed to have an IdentityProvider.");

            RuleFor(group => group)
                .Must(g => g.SourceEquals(GroupConstants.DirectorySource)
                           && !string.IsNullOrWhiteSpace(g.IdentityProvider)
                           && IdentityConstants.ValidIdentityProviders.Contains(g.IdentityProvider, StringComparer.OrdinalIgnoreCase))
                .WithMessage($"Please specify a valid IdentityProvider. Valid identity providers include the following: {string.Join(", ", IdentityConstants.ValidIdentityProviders)}");
        }
    }
}