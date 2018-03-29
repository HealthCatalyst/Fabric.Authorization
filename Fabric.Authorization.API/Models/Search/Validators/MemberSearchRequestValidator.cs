using System;
using System.Linq;

using Fabric.Authorization.Domain.Validators;

using FluentValidation;

namespace Fabric.Authorization.API.Models.Search.Validators
{
    public class MemberSearchRequestValidator : BaseSearchRequestValidator<MemberSearchRequest>
    {
        private static readonly string[] ValidSortKeys = {"name", "role", "lastlogin", "subjectid"};

        public MemberSearchRequestValidator()
        {
            ConfigureRules();
        }

        private void ConfigureRules()
        {
            RuleFor(request => request).Must(DoesContainRequiredFields)
                .WithMessage("Please specify 'client_id' OR 'grain' (for shared grain searches) but NOT both.")
                .WithState(c => ValidationEnums.ValidationState.MissingRequiredField);

            RuleFor(request => request.SortKey)
                .Must(sortKey => string.IsNullOrWhiteSpace(sortKey) ||
                                 ValidSortKeys.Contains(sortKey, StringComparer.OrdinalIgnoreCase))
                .WithMessage($"sort_key must be one of the following values: {string.Join(", ", ValidSortKeys)}")
                .WithState(c => ValidationEnums.ValidationState.InvalidFieldValue);

            RuleFor(request => request.SortDirection)
                .Must(sortDirection => string.IsNullOrWhiteSpace(sortDirection) ||
                                       ValidSortDirections.Contains(sortDirection, StringComparer.OrdinalIgnoreCase))
                .WithMessage($"sort_dir must be one of the following values: {ValidSortDirections}")
                .WithState(c => ValidationEnums.ValidationState.InvalidFieldValue);
        }

        private static bool DoesContainRequiredFields(MemberSearchRequest searchRequest)
        {
            var missingClientId = string.IsNullOrEmpty(searchRequest.ClientId);
            var missingGrain = string.IsNullOrEmpty(searchRequest.Grain);
            return missingClientId ^ missingGrain;
        }
    }
}