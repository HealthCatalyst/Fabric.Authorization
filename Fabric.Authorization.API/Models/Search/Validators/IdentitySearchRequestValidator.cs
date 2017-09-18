using System;
using System.Linq;
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
                .WithMessage("Please specify a Client ID for searching.");

            RuleFor(request => request.SortKey)
                .Must(sortKey => string.IsNullOrWhiteSpace(sortKey) || ValidSortKeys.Contains(sortKey, StringComparer.OrdinalIgnoreCase));

            RuleFor(request => request.SortDirection)
                .Must(sortDirection => string.IsNullOrWhiteSpace(sortDirection) || ValidSortDirections.Contains(sortDirection, StringComparer.OrdinalIgnoreCase));
        }
    }
}