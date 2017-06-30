using System;
using System.Collections.Generic;
using System.Text;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;
using FluentValidation;

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

        private bool BeUnique(string groupId)
        {
            return !_GroupStore.Exists(groupId);
        }
    }
}
