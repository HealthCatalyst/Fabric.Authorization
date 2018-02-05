using Nancy.TinyIoc;

namespace Fabric.Authorization.API.DependencyInjection
{
    public interface IPersistenceConfigurator
    {
        void ConfigureApplicationInstances(TinyIoCContainer container);
        void ConfigureRequestInstances(TinyIoCContainer container);
    }
}