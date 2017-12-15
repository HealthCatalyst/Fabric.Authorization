using System.Security.Claims;
using Fabric.Authorization.API;
using Fabric.Authorization.API.Configuration;
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
        public TestBootstrapper(ILogger logger, IAppConfiguration appConfig, LoggingLevelSwitch levelSwitch, IHostingEnvironment env, ClaimsPrincipal principal) : base(logger, appConfig, levelSwitch, env)
        {
            _principal = principal;
        }

        protected override void RequestStartup(TinyIoCContainer container, IPipelines pipelines, NancyContext context)
        {
            base.RequestStartup(container, pipelines, context);
            context.CurrentUser = _principal;
        }
    }
}
