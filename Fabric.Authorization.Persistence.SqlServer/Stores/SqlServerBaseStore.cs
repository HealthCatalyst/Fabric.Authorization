using System;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Persistence.SqlServer.Services;

namespace Fabric.Authorization.Persistence.SqlServer.Stores
{
    public abstract class SqlServerBaseStore
    {
        protected IAuthorizationDbContext AuthorizationDbContext;
        protected IEventService EventService;

        protected SqlServerBaseStore(IAuthorizationDbContext authorizationDbContext, IEventService eventService)
        {
            AuthorizationDbContext = authorizationDbContext ??
                                     throw new ArgumentNullException(nameof(authorizationDbContext));

            EventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        }
    }
}