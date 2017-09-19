using System;
using System.Net.Http.Headers;
using System.Security.Claims;
using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.API.Extensions;
using Fabric.Authorization.API.Infrastructure;
using Fabric.Authorization.API.Logging;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.API.Services;
using Fabric.Authorization.Domain.Events;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Domain.Stores.CouchDB;
using Fabric.Platform.Bootstrappers.Nancy;
using LibOwin;
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
        private readonly IAppConfiguration _appConfig;
        private readonly ILogger _logger;
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

            pipelines.OnError.AddItemToEndOfPipeline((ctx, ex) => HandleInternalServerError(ctx,ex, container.Resolve<IResponseNegotiator>()));

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

        private dynamic HandleInternalServerError(NancyContext context, Exception exception,
            IResponseNegotiator responseNegotiator)
        {
            _logger.Error(exception, "Unhandled error on request: @{Url}. Error Message: @{Message}", context.Request.Url,
                exception.Message);

           context.NegotiationContext = new NegotiationContext();

            var negotiator = new Negotiator(context)
                .WithStatusCode(HttpStatusCode.InternalServerError)
                .WithModel(new Error()
                {
                    Message = "There was an internal server error while processing the request.",
                    Code = ((int)HttpStatusCode.InternalServerError).ToString()
                })
                .WithHeaders(HttpResponseHeaders.CorsHeaders);


            var response = responseNegotiator.NegotiateResponse(negotiator, context);            
            return response;
        }

        private static void InitializeSwaggerMetadata()
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
            SwaggerMetadataProvider.SetSecuritySchemeBuilder(securitySchemeBuilder, "fabric.identity");
        }

        private void ConfigureRegistrations(TinyIoCContainer container)
        {
            container.Register(_appConfig);
            container.Register(_logger);
            var options = new MemoryCacheOptions();
            var eventLogger = LogFactory.CreateEventLogger(_loggingLevelSwitch, _appConfig.ApplicationInsights);
            var serilogEventWriter = new SerilogEventWriter(eventLogger);
            container.Register<IEventWriter>(serilogEventWriter, "innerEventWriter");
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