using Nancy.TinyIoc;

namespace Fabric.Authorization.API.DependencyInjection
{
    public interface IPersistenceConfigurator
    {
        void ConfigureSingletons(TinyIoCContainer container);
        void ConfigureRequestInstances(TinyIoCContainer container);
    }
}