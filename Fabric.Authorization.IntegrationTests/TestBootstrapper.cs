using System.Security.Claims;
using Fabric.Authorization.API;
using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.API.RemoteServices.Identity.Providers;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Persistence.SqlServer.Stores;
using Microsoft.AspNetCore.Hosting;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;
using Serilog;
using Serilog.Core;

namespace Fabric.Authorization.IntegrationTests
{
    public class TestBootstrapper : Bootstrapper
    {
        private readonly ClaimsPrincipal _principal;
        private readonly IIdentityServiceProvider _identityServiceProvider;

        public TestBootstrapper(ILogger logger, 
            IAppConfiguration appConfig, 
            LoggingLevelSwitch levelSwitch,
            IHostingEnvironment env, 
            ClaimsPrincipal principal, 
            IIdentityServiceProvider identityServiceProvider = null)
            : base(logger, appConfig, levelSwitch, env, null)
        {
            _principal = principal;
            _identityServiceProvider = identityServiceProvider;
        }

        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            // needed for GroupMigratorServiceTests
            container.Register<IGroupStore, SqlServerGroupStore>();
            base.ApplicationStartup(container, pipelines);
        }

        protected override void RequestStartup(TinyIoCContainer container, IPipelines pipelines, NancyContext context)
        {
            base.RequestStartup(container, pipelines, context);
            context.CurrentUser = _principal;
        }

        protected override void ConfigureRequestContainer(TinyIoCContainer container, NancyContext context)
        {
            base.ConfigureRequestContainer(container, context);
            if (_identityServiceProvider != null)
            {
                container.Register(_identityServiceProvider);
            }
        }
    }
}
