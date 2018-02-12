using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Persistence.CouchDb.Configuration;
using Fabric.Authorization.Persistence.CouchDb.Services;
using Fabric.Authorization.Persistence.CouchDb.Stores;
using Nancy.TinyIoc;

namespace Fabric.Authorization.API.DependencyInjection
{
    public class CouchDbConfigurator : IPersistenceConfigurator
    {
        private readonly IAppConfiguration _appConfiguration;

        public CouchDbConfigurator(IAppConfiguration appConfiguration)
        {
            _appConfiguration = appConfiguration;
        }

        public void ConfigureApplicationInstances(TinyIoCContainer container)
        {
            container.Register<IDocumentDbService, CouchDbAccessService>("inner");
            container.Register<ICouchDbSettings>(_appConfiguration.CouchDbSettings);
            container.Register<IDbBootstrapper>((c, p) => c.Resolve<CouchDbBootstrapper>(new NamedParameterOverloads
            {
                {"documentDbService", c.Resolve<IDocumentDbService>("inner")}
            }));
        }

        public void ConfigureRequestInstances(TinyIoCContainer container)
        {
            container.Register<IDocumentDbService>(
                (c, p) => c.Resolve<AuditingDocumentDbService>(new NamedParameterOverloads
                {
                    {"innerDocumentDbService", c.Resolve<IDocumentDbService>("inner")}
                }), "auditing");
            container.Register<IDocumentDbService>(
                (c, p) => c.Resolve<CachingDocumentDbService>(new NamedParameterOverloads
                {
                    {"innerDocumentDbService", c.Resolve<IDocumentDbService>("auditing")}
                }));

            // TODO: if other CouchDB store types need a different formatter, we'll have to register those
            container.Register<IIdentifierFormatter, IdpIdentifierFormatter>();

            container.Register<IRoleStore, CouchDbRoleStore>();
            container.Register<IUserStore, CouchDbUserStore>();
            container.Register<IPermissionStore, CouchDbPermissionStore>();
            container.Register<IGroupStore, CouchDbGroupStore>();
            container.Register<IClientStore, CouchDbClientStore>();
            container.Register<IGrainStore, CouchDbGrainStore>();
            container.Register<ISecurableItemStore, CouchDbSecurableItemStore>();
        }
    }
}
