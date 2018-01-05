using System;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
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
                .WithMessage("Please specify a Name for this Group.")
                .WithState(g => ValidationEnums.ValidationState.MissingRequiredField);

            RuleFor(group => group.Source)
                .NotEmpty()
                .WithMessage("Please specify a Source for this Group.")
                .WithState(g => ValidationEnums.ValidationState.MissingRequiredField);

            RuleFor(group => group)
                .Must(BeUnique)
                .WithMessage(g => $"An active group with groupName {g.Name} already exists. Please provide a new groupName.")
                .WithState(g => ValidationEnums.ValidationState.Duplicate);
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
            //if id is null then Name is not set which will be caught in a different validator
            if (group.Id == null)
            {
                return true;
            }

            return !_groupService.Exists(group?.Id).Result;
        }
    }
}