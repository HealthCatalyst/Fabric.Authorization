using FluentValidation;

namespace Fabric.Authorization.API.Models.Search.Validators
{
    public class BaseSearchRequestValidator<T> : AbstractValidator<T>
    {
        protected readonly string[] ValidSortDirections = { "asc", "ascedning", "desc", "descending" };
    }
}