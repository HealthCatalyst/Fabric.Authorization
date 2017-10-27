using System;
using System.Linq;

using Fabric.Authorization.Domain.Validators;

using FluentValidation;

namespace Fabric.Authorization.API.Models.Search.Validators
{
    public class IdentitySearchRequestValidator : BaseSearchRequestValidator<IdentitySearchRequest>
    {
        private static readonly string[] ValidSortKeys = {"name", "role", "lastlogin", "subjectid"};

        public IdentitySearchRequestValidator()
        {
            ConfigureRules();
        }

        private void ConfigureRules()
        {
            RuleFor(request => request.ClientId)
                .NotEmpty()
                .WithMessage("Please specify client_id for searching.")
                .WithState(c => ValidationEnums.ValidationState.MissingRequiredField);

            RuleFor(request => request.SortKey)
                .Must(sortKey => string.IsNullOrWhiteSpace(sortKey) ||
                                 ValidSortKeys.Contains(sortKey, StringComparer.OrdinalIgnoreCase))
                .WithMessage($"sort_key must be one of the following values: {ValidSortKeys}")
                .WithState(c => ValidationEnums.ValidationState.InvalidFieldValue);

            RuleFor(request => request.SortDirection)
                .Must(sortDirection => string.IsNullOrWhiteSpace(sortDirection) ||
                                       ValidSortDirections.Contains(sortDirection, StringComparer.OrdinalIgnoreCase))
                .WithMessage($"sort_dir must be one of the following values: {ValidSortDirections}")
                .WithState(c => ValidationEnums.ValidationState.InvalidFieldValue);
        }
    }
}