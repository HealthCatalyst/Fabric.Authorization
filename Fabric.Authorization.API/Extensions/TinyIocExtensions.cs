using System;
using System.Collections.Generic;
using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.API.RemoteServices.Identity.Providers;
using Fabric.Authorization.API.Services;
using Fabric.Authorization.Domain.Resolvers.Permissions;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Persistence.CouchDb.Services;
using Fabric.Authorization.Persistence.CouchDb.Stores;
using Fabric.Authorization.Persistence.InMemory.Stores;
using Fabric.Authorization.Persistence.SqlServer.Services;
using Fabric.Authorization.Persistence.SqlServer.Stores;
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
            container.Register<IPermissionResolverService, PermissionResolverService>();
            container.RegisterMultiple<IPermissionResolverService>(new List<Type>
            {
                typeof(GranularPermissionResolverService),
                typeof(RolePermissionResolverService)
            });

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

        public static TinyIoCContainer RegisterSqlServerStores(
            this TinyIoCContainer container,
            IAppConfiguration appConfiguration,
            LoggingLevelSwitch levelSwitch)
        {
            container.Register<IAuthorizationDbContext, AuthorizationDbContext>();
            container.Register<IRoleStore, SqlServerRoleStore>();
            container.Register<IUserStore, SqlServerUserStore>();
            container.Register<IPermissionStore, SqlServerPermissionStore>();
            container.Register<IGroupStore, SqlServerGroupStore>();
            container.Register<IClientStore, SqlServerClientStore>();

            return container;
        }
    }
}