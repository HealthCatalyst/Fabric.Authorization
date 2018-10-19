using System;
using System.Collections.Generic;
using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.API.RemoteServices.Identity.Providers;
using Fabric.Authorization.API.RemoteServices.IdentityProviderSearch.Providers;
using Fabric.Authorization.API.Services;
using Fabric.Authorization.Domain.Resolvers.Permissions;
using Fabric.Authorization.Domain.Services;
using Nancy.TinyIoc;

namespace Fabric.Authorization.API.Extensions
{
    public static class TinyIocExtensions
    {
        public static TinyIoCContainer RegisterServices(this TinyIoCContainer container, IAppConfiguration appConfiguration)
        {
            container.Register<RoleService, RoleService>();
            container.Register<UserService, UserService>();
            container.Register<PermissionService, PermissionService>();
            container.Register<GroupService, GroupService>();
            container.Register<ClientService, ClientService>();
            container.Register<GrainService, GrainService>();
            container.Register<SecurableItemService, SecurableItemService>();
            container.Register<MemberSearchService, MemberSearchService>();
            container.Register<IIdentityServiceProvider, IdentityServiceProvider>();
            container.Register<IIdPSearchProvider, IdPSearchProvider>();
            container.Register<IPermissionResolverService, PermissionResolverService>();
            container.RegisterMultiple<IPermissionResolverService>(new List<Type>
            {
                typeof(GranularPermissionResolverService),
                typeof(RolePermissionResolverService),
                typeof(SharedGrainPermissionResolverService)
            });

            container.Register<IEventService, EventService>();
            container.Register<IEventContextResolverService, EventContextResolverService>();
            RegisterEDWAdminRoleSyncService(container);

            return container;
        }

        private static void RegisterEDWAdminRoleSyncService(TinyIoCContainer container)
        {
            if (container.Resolve<DefaultPropertySettings>().DualStoreEDWAdminPermissions)
            {
                container.Register<IEDWAdminRoleSyncService, EDWAdminRoleSyncService>();
            }
            else
            {
                container.Register<IEDWAdminRoleSyncService, DisableEDWAdminRoleSyncService>();
            }
        }
    }
}