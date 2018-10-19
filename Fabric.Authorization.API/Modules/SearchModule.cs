using Fabric.Authorization.API.Services;
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
            AccessService accessService) : base(path, logger, abstractValidator, accessService)
        {
        }
    }
}