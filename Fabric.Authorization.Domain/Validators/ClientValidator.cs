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

        public GroupValidator(IGroupStore GroupStore)
        {
            _GroupStore = GroupStore ?? throw new ArgumentNullException(nameof(GroupStore));
            ConfigureRules();
        }

        private void ConfigureRules()
        {
            RuleFor(Group => Group.Name)
                .NotEmpty()
                .WithMessage("Please specify a Name for this Group");
        }

        private bool BeUnique(string GroupId)
        {
            return !_GroupStore.GroupExists(GroupId);
        }
    }
}
