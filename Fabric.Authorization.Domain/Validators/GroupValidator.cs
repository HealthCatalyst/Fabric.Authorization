using System;
using System.Linq;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using FluentValidation;

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
                .Must(g => GroupConstants.ValidGroupSources.Contains(g.Source, StringComparer.OrdinalIgnoreCase))
                .WithMessage($"Please specify a valid GroupSource. Valid group sources include the following: {string.Join(", ", GroupConstants.ValidGroupSources)}");

            RuleFor(group => group)
                .Must(g => string.IsNullOrWhiteSpace(g.IdentityProvider))
                .When(g => g.SourceEquals(GroupConstants.CustomSource))
                .WithMessage("Custom groups are not allowed to have an IdentityProvider.");

            RuleFor(group => group)
                .Must(g => !string.IsNullOrWhiteSpace(g.IdentityProvider)
                           && IdentityConstants.ValidIdentityProviders.Contains(g.IdentityProvider, StringComparer.OrdinalIgnoreCase))
                .When(g => g.SourceEquals(GroupConstants.DirectorySource))
                .WithMessage($"Please specify a valid IdentityProvider. Valid identity providers include the following: {string.Join(", ", IdentityConstants.ValidIdentityProviders)}");
        }
    }
}