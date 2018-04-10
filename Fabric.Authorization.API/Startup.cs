using System;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Infrastructure;
using Fabric.Authorization.API.Services;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Persistence.CouchDb.Services;
using Fabric.Authorization.Persistence.CouchDb.Stores;
using Fabric.Authorization.Persistence.InMemory.Stores;
using Fabric.Platform.Auth;
using Fabric.Platform.Logging;
using Fabric.Platform.Shared.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nancy;
using Nancy.Owin;
using Serilog;
using Serilog.Core;
using ILogger = Serilog.ILogger;

namespace Fabric.Authorization.API
{
    public class Startup
    {
        private readonly IAppConfiguration _appConfig;
        private readonly LoggingLevelSwitch _levelSwitch;
        private readonly ILogger _logger;
        private readonly IdentityServerConfidentialClientSettings _idServerSettings;

        public Startup(IHostingEnvironment env)
        {
            _appConfig = new AuthorizationConfigurationProvider(new WindowsCertificateService()).GetAppConfiguration(env.ContentRootPath);

            _levelSwitch = new LoggingLevelSwitch();
            _idServerSettings = _appConfig.IdentityServerConfidentialClientSettings;
            _logger = Logging.LogFactory.CreateTraceLogger(_levelSwitch, _appConfig.ApplicationInsights);

        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddWebEncoders();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddSerilog(_logger);
            
            app.UseIdentityServerAuthentication(new IdentityServerAuthenticationOptions
            {
                Authority = _idServerSettings.Authority,
                RequireHttpsMetadata = false,
                ApiName = _idServerSettings.ClientId
            });

            app.UseStaticFiles()
                .UseOwin()
                .UseFabricLoggingAndMonitoring(_logger, HealthCheck, _levelSwitch)
                .UseAuthPlatform(_idServerSettings.Scopes, _allowedPaths)
                .UseNancy(opt => opt.Bootstrapper = new Bootstrapper(_logger, _appConfig, _levelSwitch, env, loggerFactory));
        }

        // TODO: make this DB-agnostic
        public async Task<bool> HealthCheck()
        {
            var eventContextResolverService = new EventContextResolverService(new NancyContextWrapper(new NancyContext()));
            var clientStore = _appConfig.StorageProvider.Equals(StorageProviders.InMemory, StringComparison.OrdinalIgnoreCase)
                ? (IClientStore)new InMemoryClientStore()
                : new CouchDbClientStore(new CouchDbAccessService(_appConfig.CouchDbSettings, _logger), _logger, eventContextResolverService);
            
            var result = await clientStore.GetAll();
            return result.Any();
        }

        private readonly string[] _allowedPaths =
        {
            "/swagger/ui/index",
            "/swagger/ui/index.html",
            "/swagger/ui/swagger.json"
        };
    }
}
