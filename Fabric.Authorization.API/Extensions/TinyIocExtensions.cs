using System;
using System.Collections.Generic;
using Fabric.Authorization.API.RemoteServices.Identity.Providers;
using Fabric.Authorization.API.Services;
using Fabric.Authorization.Domain.Resolvers.Permissions;
using Fabric.Authorization.Domain.Services;
using Nancy.TinyIoc;

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

            container.Register<IEventService, EventService>();
            container.Register<IEventContextResolverService, EventContextResolverService>();

            return container;
        }
    }
}