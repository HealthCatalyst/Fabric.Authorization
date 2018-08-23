using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.Persistence.SqlServer.Services;
using Fabric.Authorization.Persistence.SqlServer.Stores.EDW;
using Nancy.TinyIoc;

namespace Fabric.Authorization.API.DependencyInjection
{
    public class SqlServerConfigurator : BaseSqlServerConfigurator
    {
        public SqlServerConfigurator(IAppConfiguration appConfiguration) : base(appConfiguration)
        {
        }

        protected override TinyIoCContainer.RegisterOptions RegisterDatabaseContext(TinyIoCContainer container)
        {
            return container.Register<IAuthorizationDbContext, AuthorizationDbContext>();
        }

        protected override TinyIoCContainer.RegisterOptions RegisterEDWDatabaseContext(TinyIoCContainer container)
        {
            return container.Register<ISecurityContext, SecurityContext>();
        }
    }
}