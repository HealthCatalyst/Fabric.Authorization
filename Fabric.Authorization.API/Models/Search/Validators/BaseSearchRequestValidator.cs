using System.Collections.Generic;
using System.Linq;
using Catalyst.Fabric.Authorization.Models.Search;
using FluentValidation;

namespace Fabric.Authorization.API.Models.Search.Validators
{
    public class BaseSearchRequestValidator<T> : AbstractValidator<T>
    {
        protected readonly IEnumerable<string> ValidSortDirections;

        public BaseSearchRequestValidator()
        {
            ValidSortDirections = SearchConstants.AscendingSortKeys.Concat(SearchConstants.DescendingSortKeys);
        }
    }
}