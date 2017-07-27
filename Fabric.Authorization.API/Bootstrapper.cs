using System;
using System.Security.Claims;
using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.API.Extensions;
using Fabric.Platform.Bootstrappers.Nancy;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Owin;
using Nancy.TinyIoc;
using Serilog;
using LibOwin;
using Serilog.Core;

namespace Fabric.Authorization.API
{
    public class Bootstrapper : DefaultNancyBootstrapper
    {
        private readonly ILogger _logger;
        private readonly IAppConfiguration _appConfig;
        private readonly LoggingLevelSwitch _loggingLevelSwitch;

        public Bootstrapper(ILogger logger, IAppConfiguration appConfig, LoggingLevelSwitch levelSwitch)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
            _loggingLevelSwitch = levelSwitch ?? throw new ArgumentNullException(nameof(levelSwitch));
        }

        protected override void RequestStartup(TinyIoCContainer container, IPipelines pipelines, NancyContext context)
        {
            base.RequestStartup(container, pipelines, context);
            var owinEnvironment = context.GetOwinEnvironment();
            var principal = owinEnvironment[OwinConstants.RequestUser] as ClaimsPrincipal;
            context.CurrentUser = principal;
            var appConfig = container.Resolve<IAppConfiguration>();
            container.UseHttpClientFactory(context, appConfig.IdentityServerConfidentialClientSettings);
        }

        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);
            pipelines.OnError.AddItemToEndOfPipeline((ctx, ex) =>
            {
                _logger.Error(ex, "Unhandled error on request: @{Url}. Error Message: @{Message}", ctx.Request.Url,
                    ex.Message);
                return ctx.Response;
            });

            pipelines.AfterRequest += ctx =>
            {
                ctx.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                ctx.Response.Headers.Add("Access-Control-Allow-Headers",
                    "Origin, X-Requested-With, Content-Type, Accept, Authorization");
                ctx.Response.Headers.Add("Access-Control-Allow-Methods", "POST, GET, PUT, DELETE, OPTIONS");
            };

            //container registrations
            container.Register(_appConfig);
            container.Register(_logger);
            RegisterStores(container);
            container.RegisterServices();
        }

        protected void ApplicationStartupForTests(TinyIoCContainer container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);
            pipelines.OnError.AddItemToEndOfPipeline((ctx, ex) =>
            {
                _logger.Error(ex, "Unhandled error on request: @{Url}. Error Message: @{Message}", ctx.Request.Url, ex.Message);
                return ctx.Response;
            });

            pipelines.AfterRequest += ctx =>
            {
                ctx.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                ctx.Response.Headers.Add("Access-Control-Allow-Headers", "Origin, X-Requested-With, Content-Type, Accept, Authorization");
                ctx.Response.Headers.Add("Access-Control-Allow-Methods", "POST, GET, PUT, DELETE, OPTIONS");
            };

            //container registrations
            container.Register(_appConfig);
            container.Register(_logger);
            container.RegisterServices();
            container.RegisterInMemoryStores();
        }

        private void RegisterStores(TinyIoCContainer container)
        {
            if (_appConfig.UseInMemoryStores)
            {
                container.RegisterInMemoryStores();
            }
            else
            {
                container.RegisterCouchDbStores(_appConfig, _loggingLevelSwitch);
            }
        }
    }

    
}
