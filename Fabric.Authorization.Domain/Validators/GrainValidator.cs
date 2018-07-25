using Fabric.Authorization.Domain.Models;
using FluentValidation;

namespace Fabric.Authorization.Domain.Validators
{
    public class GrainValidator : AbstractValidator<Grain>
    {
        public GrainValidator()
        {
            ConfigureRules();
        }

        private void ConfigureRules()
        {
            RuleFor(item => item.Name)
                .NotEmpty()
                .WithMessage("Please specify a Name for the Grain.")
                .WithState(s => ValidationEnums.ValidationState.MissingRequiredField);
        }
    }
}
