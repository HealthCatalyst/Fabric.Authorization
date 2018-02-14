using System;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using FluentValidation;

namespace Fabric.Authorization.Domain.Validators
{
    public class UserValidator : AbstractValidator<User>
    {
        private readonly UserService _userService;

        public UserValidator(UserService userService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            ConfigRules();
        }

        private void ConfigRules()
        {
            RuleFor(user => user.SubjectId)
                .NotEmpty()
                .WithMessage("You must specify a SubjectId for this user")
                .WithState(u => ValidationEnums.ValidationState.MissingRequiredField);

            RuleFor(user => user.IdentityProvider)
                .NotEmpty()
                .WithMessage("You must specify an IdentityProvider for this user")
                .WithState(u => ValidationEnums.ValidationState.MissingRequiredField);

            RuleFor(user => user)
                .Must(BeUnique)
                .When(user => !string.IsNullOrEmpty(user.SubjectId) && !string.IsNullOrEmpty(user.IdentityProvider))
                .WithMessage(
                    user =>
                        $"The User {user.SubjectId} already exists for the Identity Provider: {user.IdentityProvider}")
                .WithState(user => ValidationEnums.ValidationState.Duplicate);
        }

        private bool BeUnique(User user)
        {
            var exists = Task.Run(async () => await _userService.Exists(user.SubjectId, user.IdentityProvider)).Result;
            return !exists;
        }
    }
}
