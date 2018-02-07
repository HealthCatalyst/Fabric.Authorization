using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Persistence.InMemory.Services;
using Fabric.Authorization.Persistence.InMemory.Stores;
using Nancy.TinyIoc;

namespace Fabric.Authorization.API.DependencyInjection
{
    public class InMemoryConfigurator : IPersistenceConfigurator
    {
        public void ConfigureApplicationInstances(TinyIoCContainer container)
        {
            container.Register<IDbBootstrapper, InMemoryDbBootstrapper>();
            container.Register<IIdentifierFormatter, IdpIdentifierFormatter>();
            container.Register<IRoleStore, InMemoryRoleStore>();
            container.Register<IUserStore, InMemoryUserStore>();
            container.Register<IPermissionStore, InMemoryPermissionStore>();
            container.Register<IGroupStore, InMemoryGroupStore>();
            container.Register<IClientStore, InMemoryClientStore>();
            container.Register<IGrainStore, InMemoryGrainStore>();
        }

        public void ConfigureRequestInstances(TinyIoCContainer container)
        {
        }
    }
}
