using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.API.RemoteServices.Identity.Providers;
using Fabric.Authorization.API.Services;
using Fabric.Authorization.Domain.Events;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Domain.Stores.CouchDB;
using Fabric.Authorization.Domain.Stores.InMemory;
using Fabric.Authorization.Domain.Stores.Services;
using Nancy.TinyIoc;
using Serilog.Core;

namespace Fabric.Authorization.API.Extensions
{
    public static class TinyIocExtensions
    {
        public static TinyIoCContainer RegisterServices(this TinyIoCContainer container)
        {
            container.Register<RoleService, RoleService>();
            container.Register<UserService, UserService>();
            container.Register<PermissionService, PermissionService>();
            container.Register<GroupService, GroupService>();
            container.Register<ClientService, ClientService>();
            container.Register<SecurableItemService, SecurableItemService>();
            container.Register<IdentitySearchService, IdentitySearchService>();
            container.Register<IIdentityServiceProvider, IdentityServiceProvider>();

            return container;
        }

        public static TinyIoCContainer RegisterInMemoryStores(this TinyIoCContainer container)
        {
            container.Register<IIdentifierFormatter, IdpIdentifierFormatter>();
            container.Register<IRoleStore, InMemoryRoleStore>();
            container.Register<IUserStore, InMemoryUserStore>();
            container.Register<IPermissionStore, InMemoryPermissionStore>();
            container.Register<IGroupStore, InMemoryGroupStore>();
            container.Register<IClientStore, InMemoryClientStore>();

            return container;
        }

        public static TinyIoCContainer RegisterCouchDbStores(this TinyIoCContainer container,
            IAppConfiguration appConfiguration, LoggingLevelSwitch levelSwitch)
        {
            container.Register<IEventService, EventService>();
            container.Register<IEventContextResolverService, EventContextResolverService>();
            container.Register<IEventWriter>(
                (c, p) => c.Resolve<CouchDbEventWriter>(new NamedParameterOverloads
                    {
                        {"documentDbService", c.Resolve<IDocumentDbService>("inner")},
                        {"innerEventWriter", c.Resolve<IEventWriter>("innerEventWriter")}
                    }
                )
            );
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

            return container;
        }
    }
}