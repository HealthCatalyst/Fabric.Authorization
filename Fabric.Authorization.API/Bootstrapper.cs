using System;
using System.Security.Claims;
using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.API.Extensions;
using Fabric.Authorization.API.Infrastructure;
using Fabric.Authorization.API.Logging;
using Fabric.Authorization.API.Services;
using Fabric.Authorization.Domain.Events;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Domain.Stores.CouchDB;
using Fabric.Platform.Bootstrappers.Nancy;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Owin;
using Nancy.TinyIoc;
using Serilog;
using LibOwin;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
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

        protected override void ConfigureRequestContainer(TinyIoCContainer container, NancyContext context)
        {
            base.ConfigureRequestContainer(container, context);
            container.Register(new NancyContextWrapper(context));
            container.RegisterServices();
            if (!_appConfig.UseInMemoryStores)
            {
                container.RegisterCouchDbStores(_appConfig, _loggingLevelSwitch);
            }
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

            pipelines.BeforeRequest += ctx => RequestHooks.SetDefaultVersionInUrl(ctx);

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
            var options = new MemoryCacheOptions();
            var eventLogger = LogFactory.CreateEventLogger(_loggingLevelSwitch, _appConfig.ApplicationInsights);
            var serilogEventWriter = new SerilogEventWriter(eventLogger);
            container.Register<IEventWriter>(serilogEventWriter, "innerEventWriter");
            container.Register(options);
            container.Register<ICouchDbSettings>(_appConfig.CouchDbSettings);
            container.Register(typeof(IOptions<>), typeof(OptionsManager<>));
            container.Register<IMemoryCache, MemoryCache>();
            if (_appConfig.UseInMemoryStores)
            {
                container.RegisterInMemoryStores();
            }
            else
            {
                container.Register<IDocumentDbService, CouchDbAccessService>("inner");
                var dbAccessService = container.Resolve<CouchDbAccessService>();
                dbAccessService.Initialize().Wait();
                dbAccessService.AddViews("roles", CouchDbRoleStore.GetViews()).Wait();
                dbAccessService.AddViews("permissions", CouchDbPermissionStore.GetViews()).Wait();
            }
        }

        protected void ApplicationStartupForTests(TinyIoCContainer container, IPipelines pipelines)
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
            container.Register<NancyContextWrapper>();
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
