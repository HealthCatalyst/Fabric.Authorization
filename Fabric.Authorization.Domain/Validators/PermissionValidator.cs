using FluentValidation;

namespace Fabric.Authorization.Domain.Validators
{
    public class PermissionValidator : AbstractValidator<Permission>
    {
        public PermissionValidator()
        {
            RuleFor(permission => permission.Grain)
                .NotEmpty()
                .WithMessage("Please specify a Grain for this permission");

            RuleFor(permission => permission.Resource)
                .NotEmpty()
                .WithMessage("Please specify a Resource for this permission");

            RuleFor(permission => permission.Name)
                .NotEmpty()
                .WithMessage("Please specify a Name for this permission");
        }
    }
}
