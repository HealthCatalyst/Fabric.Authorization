using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.API.Services;
using Fabric.Authorization.Domain.Events;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores;
using Nancy.TinyIoc;

namespace Fabric.Authorization.API.Extensions
{
    public static class TinyIocExtensions
    {
        public static TinyIoCContainer RegisterServices(this TinyIoCContainer container)
        {
            container.Register<RoleService, RoleService>();
            container.Register<PermissionService, PermissionService>();
            container.Register<GroupService, GroupService>();
            container.Register<ClientService, ClientService>();
            container.Register<SecurableItemService, SecurableItemService>();

            return container;
        }

        public static TinyIoCContainer RegisterInMemoryStores(this TinyIoCContainer container)
        {
            container.Register<IRoleStore, InMemoryRoleStore>();
            container.Register<IPermissionStore, InMemoryPermissionStore>();
            container.Register<IGroupStore, InMemoryGroupStore>();
            container.Register<IClientStore, InMemoryClientStore>();

            return container;
        }

        public static TinyIoCContainer RegisterCouchDbStores(this TinyIoCContainer container, ICouchDbSettings couchDbSettings)
        {
            container.Register(couchDbSettings);
            container.Register<IEventService, EventService>();
            container.Register<IEventContextResolverService, EventContextResolverService>();
            container.Register<IEventWriter, SerilogEventWriter>();
            container.Register<IDocumentDbService, CouchDbAccessService>("inner");
            container.Register<IDocumentDbService>(
                (c, p) => c.Resolve<AuditingDocumentDbService>(new NamedParameterOverloads
                {
                    {"innerDocumentDbService", c.Resolve<IDocumentDbService>("inner")}
                }));
            container.Register<IRoleStore, CouchDBRoleStore>();
            container.Register<IPermissionStore, CouchDBPermissionStore>();
            container.Register<IGroupStore, CouchDBGroupStore>();
            container.Register<IClientStore, CouchDBClientStore>();

            return container;
        }
    }
}
