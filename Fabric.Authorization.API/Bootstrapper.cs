using System;
using System.Security.Claims;
using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.API.Converters;
using Fabric.Authorization.API.Extensions;
using Fabric.Authorization.API.Infrastructure;
using Fabric.Authorization.API.Infrastructure.PipelineHooks;
using Fabric.Authorization.API.Logging;
using Fabric.Authorization.API.Services;
using Fabric.Authorization.Domain.Events;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Domain.Stores.CouchDB;
using Fabric.Platform.Bootstrappers.Nancy;
using LibOwin;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Conventions;
using Nancy.Owin;
using Nancy.Responses.Negotiation;
using Nancy.Swagger.Services;
using Nancy.TinyIoc;
using Serilog;
using Serilog.Core;
using Swagger.ObjectModel;
using Swagger.ObjectModel.Builders;
using HttpResponseHeaders = Fabric.Authorization.API.Constants.HttpResponseHeaders;

namespace Fabric.Authorization.API
{
    public class Bootstrapper : DefaultNancyBootstrapper
    {
        private readonly IHostingEnvironment _env;
        private readonly IAppConfiguration _appConfig;
        private readonly ILogger _logger;
        private readonly LoggingLevelSwitch _loggingLevelSwitch;

        public Bootstrapper(ILogger logger, IAppConfiguration appConfig, LoggingLevelSwitch levelSwitch, IHostingEnvironment env)
        {
            _env = env;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
            _loggingLevelSwitch = levelSwitch ?? throw new ArgumentNullException(nameof(levelSwitch));
        }

        protected override Func<ITypeCatalog, NancyInternalConfiguration> InternalConfiguration =>
            NancyInternalConfiguration.WithOverrides(config => config.FieldNameConverter = typeof(UnderscoredFieldNameConverter));

        protected override void RequestStartup(TinyIoCContainer container, IPipelines pipelines, NancyContext context)
        {
            base.RequestStartup(container, pipelines, context);
            var owinEnvironment = context.GetOwinEnvironment();
            if (owinEnvironment != null)
            {
                var principal = owinEnvironment[OwinConstants.RequestUser] as ClaimsPrincipal;
                context.CurrentUser = principal;
            }
        }

        protected override void ConfigureRequestContainer(TinyIoCContainer container, NancyContext context)
        {
            base.ConfigureRequestContainer(container, context);
            container.Register(new NancyContextWrapper(context));
            var appConfig = container.Resolve<IAppConfiguration>();
            container.UseHttpClientFactory(context, appConfig.IdentityServerConfidentialClientSettings);
            container.RegisterServices();
            if (!_appConfig.UseInMemoryStores)
                container.RegisterCouchDbStores(_appConfig, _loggingLevelSwitch);
        }
        
        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            InitializeSwaggerMetadata();

            base.ApplicationStartup(container, pipelines);

            pipelines.OnError.AddItemToEndOfPipeline(
                (ctx, ex) => new OnErrorHooks(_logger)
                    .HandleInternalServerError(
                        ctx,
                        ex,
                        container.Resolve<IResponseNegotiator>(),
                        _env));

            pipelines.BeforeRequest += ctx => RequestHooks.RemoveContentTypeHeaderForGet(ctx);
            pipelines.BeforeRequest += ctx => RequestHooks.ErrorResponseIfContentTypeMissingForPostAndPut(ctx);
            pipelines.BeforeRequest += ctx => RequestHooks.SetDefaultVersionInUrl(ctx);

            pipelines.AfterRequest += ctx =>
            {
                foreach (var corsHeader in HttpResponseHeaders.CorsHeaders)
                {
                    ctx.Response.Headers.Add(corsHeader.Item1, corsHeader.Item2);
                }
            };

            ConfigureRegistrations(container);
        }

        private void InitializeSwaggerMetadata()
        {
            SwaggerMetadataProvider.SetInfo("Fabric Authorization API", "v1",
                "Fabric.Authorization contains a set of APIs that allow client applications to manage roles and permissions for users.");

            var securitySchemeBuilder = new Oauth2SecuritySchemeBuilder();
            securitySchemeBuilder.Flow(Oauth2Flows.Implicit);
            securitySchemeBuilder.Description("Authentication with Fabric.Identity");
            securitySchemeBuilder.AuthorizationUrl(@"http://localhost:5001");
            securitySchemeBuilder.Scope("fabric/authorization.read", "Grants read access to fabric.authorization resources.");
            securitySchemeBuilder.Scope("fabric/authorization.write", "Grants write access to fabric.authorization resources.");
            securitySchemeBuilder.Scope("fabric/authorization.manageclients", "Grants 'manage clients' access to fabric.authorization resources.");
            try
            {
                SwaggerMetadataProvider.SetSecuritySchemeBuilder(securitySchemeBuilder, "fabric.identity");
            }
            catch (ArgumentException ex)
            {
                _logger.Warning("Error configuring Swagger Security Scheme. {exceptionMessage}", ex.Message);
            }
            catch (NullReferenceException ex)
            {
                _logger.Warning("Error configuring Swagger Security Scheme: {exceptionMessage", ex.Message);
            }
        }

        private void ConfigureRegistrations(TinyIoCContainer container)
        {
            container.Register(_appConfig);
            container.Register(_logger);
            var options = new MemoryCacheOptions();
            var eventLogger = LogFactory.CreateEventLogger(_loggingLevelSwitch, _appConfig.ApplicationInsights);
            var serilogEventWriter = new SerilogEventWriter(eventLogger);
            container.Register<IEventWriter>(serilogEventWriter);
            container.Register(options);
            container.Register<ICouchDbSettings>(_appConfig.CouchDbSettings);
            container.Register<IPropertySettings>(_appConfig.DefaultPropertySettings);
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
                dbAccessService.SetupDefaultUser().Wait();
                dbAccessService.AddViews("roles", CouchDbRoleStore.GetViews()).Wait();
                dbAccessService.AddViews("permissions", CouchDbPermissionStore.GetViews()).Wait();
            }
        }

        protected void ApplicationStartupForTests(TinyIoCContainer container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);
            pipelines.OnError.AddItemToEndOfPipeline(
                (ctx, ex) => new OnErrorHooks(_logger)
                    .HandleInternalServerError(
                        ctx,
                        ex,
                        container.Resolve<IResponseNegotiator>(),
                        _env));

            pipelines.AfterRequest += ctx =>
            {
                foreach (var corsHeader in HttpResponseHeaders.CorsHeaders)
                {
                    ctx.Response.Headers.Add(corsHeader.Item1, corsHeader.Item2);
                }
            };

            //container registrations
            container.Register(_appConfig);
            container.Register(_logger);
            container.Register<NancyContextWrapper>();
            container.RegisterServices();
            container.RegisterInMemoryStores();
        }

        protected override void ConfigureConventions(NancyConventions nancyConventions)
        {
            base.ConfigureConventions(nancyConventions);

            nancyConventions.StaticContentsConventions.Add(
                StaticContentConventionBuilder.AddDirectory("/swagger"));
        }

        private void RegisterStores(TinyIoCContainer container)
        {
            if (_appConfig.UseInMemoryStores)
                container.RegisterInMemoryStores();
            else
                container.RegisterCouchDbStores(_appConfig, _loggingLevelSwitch);
        }
    }    
}