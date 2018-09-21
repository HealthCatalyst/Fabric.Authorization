using System;
using System.Net.Http;
using System.Security.Claims;
using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Converters;
using Fabric.Authorization.API.DependencyInjection;
using Fabric.Authorization.API.Extensions;
using Fabric.Authorization.API.Infrastructure;
using Fabric.Authorization.API.Infrastructure.PipelineHooks;
using Fabric.Authorization.API.Logging;
using Fabric.Authorization.Domain.Events;
using Fabric.Authorization.Domain.Services;
using Fabric.Platform.Bootstrappers.Nancy;
using LibOwin;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Conventions;
using Nancy.Owin;
using Nancy.Responses.Negotiation;
using Nancy.Swagger.Services;
using Nancy.TinyIoc;
using Serilog.Core;
using Swagger.ObjectModel;
using Swagger.ObjectModel.Builders;
using HttpResponseHeaders = Fabric.Authorization.API.Constants.HttpResponseHeaders;
using ILogger = Serilog.ILogger;

namespace Fabric.Authorization.API
{
    public class Bootstrapper : DefaultNancyBootstrapper
    {
        private readonly IHostingEnvironment _env;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IAppConfiguration _appConfig;
        private readonly ILogger _logger;
        private readonly LoggingLevelSwitch _loggingLevelSwitch;
        public TinyIoCContainer TinyIoCContainer { get; private set; }

        public Bootstrapper(ILogger logger, IAppConfiguration appConfig, LoggingLevelSwitch levelSwitch, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            _env = env;
            _loggerFactory = loggerFactory;
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
            container.UseHttpRequestMessageFactory(context, appConfig.IdentityServerConfidentialClientSettings);
            container.RegisterServices(appConfig);

            var configurator = container.Resolve<IPersistenceConfigurator>();
            configurator.ConfigureRequestInstances(container);
        }
        
        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            TinyIoCContainer = container;

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

            ConfigureApplicationRegistrations(container);
            var dbBootstrapper = container.Resolve<IDbBootstrapper>();
            dbBootstrapper.Setup();
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
            securitySchemeBuilder.Scope("fabric/authorization.dos.write", "Grants write access to fabric.authorization.dos resources.");
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

        private void ConfigureApplicationRegistrations(TinyIoCContainer container)
        {
            container.Register(_appConfig);
            container.Register(_loggerFactory);
            container.Register(_logger);
            var options = new MemoryCacheOptions();
            var eventLogger = LogFactory.CreateEventLogger(_loggingLevelSwitch, _appConfig);
            var serilogEventWriter = new SerilogEventWriter(eventLogger);
            container.Register<IEventWriter>(serilogEventWriter);
            container.Register(options);
            container.Register<IPropertySettings>(_appConfig.DefaultPropertySettings);
            container.Register(typeof(IOptions<>), typeof(OptionsManager<>));
            container.Register<IMemoryCache, MemoryCache>();
            container.Register<Domain.Defaults.Authorization>();

            var httpClient = new HttpClient();
            container.Register(httpClient);

            container.Register<IPersistenceConfigurator>((c, p) =>
            {
                switch (_appConfig.StorageProvider.ToLowerInvariant())
                {
                    case StorageProviders.InMemory:
                        return new InMemoryConfigurator(_appConfig);

                    case StorageProviders.SqlServer:
                        return new SqlServerConfigurator(_appConfig);

                    default:
                        throw new ConfigurationException($"Invalid configuration for StorageProvider: {_appConfig.StorageProvider}. Valid storage providers are: {StorageProviders.InMemory}, {StorageProviders.CouchDb}, {StorageProviders.SqlServer}");
                }
            });

            var configurator = container.Resolve<IPersistenceConfigurator>();
            configurator.ConfigureApplicationInstances(container);
        }

        protected override void ConfigureConventions(NancyConventions nancyConventions)
        {
            base.ConfigureConventions(nancyConventions);

            nancyConventions.StaticContentsConventions.Add(
                StaticContentConventionBuilder.AddDirectory("/swagger/ui"));
            nancyConventions.StaticContentsConventions.Add(
                AngularConventionBuilder.AddAngularRoot(AccessControl.Path, "index.html"));
        }
    }
}
