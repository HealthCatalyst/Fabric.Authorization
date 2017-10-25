using System;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores.Services;
using FluentValidation;

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

            RuleFor(group => group)
                .Must(BeUnique)
                .WithMessage("An active group with this groupName already exists. Please specify a different groupName.");
        }

        /// <summary>
        /// This ensures an active group with the same name does not already exist. It checks the Id, which is
        /// derived from the Name (Name + unique identifier). The check first attempts an exact match. If an
        /// exact match is not found, it checks if any groups exist that have an ID that starts with the 
        /// </summary>
        /// <param name="group">Incoming group to be validated</param>
        /// <returns>true if supplied group name does not exist on an active group document; otherwise false</returns>
        private bool BeUnique(Group group)
        {
            return !string.IsNullOrWhiteSpace(group?.Id) && !_groupService.Exists(group?.Id).Result;
        }
    }
}