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
                .WithMessage("Please specify client_id for searching.");

            RuleFor(request => request.SortKey)
                .Must(sortKey => string.IsNullOrWhiteSpace(sortKey) ||
                                 ValidSortKeys.Contains(sortKey, StringComparer.OrdinalIgnoreCase))
                .WithMessage($"sort_key must be one of the following values: {ValidSortKeys}");

            RuleFor(request => request.SortDirection)
                .Must(sortDirection => string.IsNullOrWhiteSpace(sortDirection) ||
                                       ValidSortDirections.Contains(sortDirection, StringComparer.OrdinalIgnoreCase))
                .WithMessage($"sort_dir must be one of the following values: {ValidSortDirections}");

            RuleFor(request => request.PageSize)
                .Must(pageSize => pageSize == null || pageSize is int)
                .WithMessage("page_size must be a valid number.");

            RuleFor(request => request.PageNumber)
                .Must(pageNumber => pageNumber == null || pageNumber is int)
                .WithMessage("page_number must be a valid number.");
        }
    }
}