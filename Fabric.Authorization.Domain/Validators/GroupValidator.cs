using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;
using FluentValidation;
using System;
using System.Threading.Tasks;

namespace Fabric.Authorization.Domain.Validators
{
    public class GroupValidator : AbstractValidator<Group>
    {
        private readonly IGroupStore _GroupStore;

        public GroupValidator(IGroupStore groupStore)
        {
            _GroupStore = groupStore ?? throw new ArgumentNullException(nameof(groupStore));
            ConfigureRules();
        }

        private void ConfigureRules()
        {
            RuleFor(Group => Group.Name)
                .NotEmpty()
                .WithMessage("Please specify a Name for this Group");
        }

        private async Task<bool> BeUnique(string groupId)
        {
            return !await _GroupStore.Exists(groupId);
        }
    }    
}
