using Fabric.Authorization.Domain.Models;
using FluentValidation;
using System;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Stores.Services;

namespace Fabric.Authorization.Domain.Validators
{
    public class GroupValidator : AbstractValidator<Group>
    {
        private readonly GroupService _groupService;

        public GroupValidator(GroupService groupService)
        {
            _groupService = groupService ?? throw new ArgumentNullException(nameof(groupService));
            ConfigureRules();
        }

        private void ConfigureRules()
        {
            RuleFor(group => group.Name)
                .NotEmpty()
                .WithMessage("Please specify a Name for this Group.");

            RuleFor(group => group.Source)
                .NotEmpty()
                .WithMessage("Please specify a Source for this Group.");
        }

        private async Task<bool> BeUnique(string groupId)
        {
            return !await _groupService.Exists(groupId);
        }
    }    
}
