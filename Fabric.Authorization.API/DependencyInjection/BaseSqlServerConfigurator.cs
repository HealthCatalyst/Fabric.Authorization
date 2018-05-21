using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Persistence.SqlServer.Services;
using Fabric.Authorization.Persistence.SqlServer.Stores;
using Nancy.TinyIoc;

namespace Fabric.Authorization.API.DependencyInjection
{
    public abstract class BaseSqlServerConfigurator : IPersistenceConfigurator
    {
        protected readonly IAppConfiguration AppConfiguration;

        protected BaseSqlServerConfigurator(IAppConfiguration appConfiguration)
        {
            AppConfiguration = appConfiguration;
        }

        public void ConfigureApplicationInstances(TinyIoCContainer container)
        {
            container.Register<IDbBootstrapper, SqlServerDbBootstrapper>().AsMultiInstance();
            RegisterDatabaseContext(container).AsMultiInstance();
            container.Register<IEventContextResolverService, NoOpEventContextResolverService>().AsMultiInstance();
            container.Register<IEventService, EventService>().AsMultiInstance();
            container.Register<IGrainStore, SqlServerGrainStore>().AsMultiInstance();
            container.Register<IClientStore, SqlServerClientStore>().AsMultiInstance();
            container.Register(AppConfiguration.ConnectionStrings);
        }

        public void ConfigureRequestInstances(TinyIoCContainer container)
        {
            RegisterDatabaseContext(container);
            container.Register<IRoleStore, SqlServerRoleStore>();
            container.Register<IUserStore, SqlServerUserStore>();
            container.Register<IPermissionStore, SqlServerPermissionStore>();
            container.Register<IGroupStore, SqlServerGroupStore>();
            container.Register<IClientStore, SqlServerClientStore>();
            container.Register<IGrainStore, SqlServerGrainStore>();
            container.Register<ISecurableItemStore, SqlServerSecurableItemStore>();
        }

        protected abstract TinyIoCContainer.RegisterOptions RegisterDatabaseContext(TinyIoCContainer container);
    }
}