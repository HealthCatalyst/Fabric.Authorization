using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.Persistence.SqlServer.Services;
using Fabric.Authorization.Persistence.SqlServer.Stores.EDW;
using Nancy.TinyIoc;
using System;

namespace Fabric.Authorization.API.DependencyInjection
{
    public class InMemoryConfigurator : BaseSqlServerConfigurator
    {
        public InMemoryConfigurator(IAppConfiguration appConfiguration) : base(appConfiguration)
        {
        }

        protected override TinyIoCContainer.RegisterOptions RegisterDatabaseContext(TinyIoCContainer container)
        {
            return container.Register<IAuthorizationDbContext, InMemoryAuthorizationDbContext>();
        }

        protected override TinyIoCContainer.RegisterOptions RegisterEDWDatabaseContext(TinyIoCContainer container)
        {
            return container.Register<ISecurityContext, InMemorySecurityContext>();
        }
    }
}