using Fabric.Authorization.API.Configuration;
using FluentValidation;
using Serilog;

namespace Fabric.Authorization.API.Modules
{
    public abstract class SearchModule<T> : FabricModule<T>
    {
        protected SearchModule()
        {
        }

        protected SearchModule(
            string path,
            ILogger logger,
            AbstractValidator<T> abstractValidator,
            IPropertySettings propertySettings = null) : base(path, logger, abstractValidator, propertySettings)
        {
        }
    }
}