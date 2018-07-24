using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Services;
using Fabric.Authorization.Domain.Stores;
using Fabric.Platform.Auth;
using Fabric.Platform.Logging;
using Fabric.Platform.Shared.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nancy.Owin;
using Serilog;
using Serilog.Core;
using ILogger = Serilog.ILogger;
using LogFactory = Fabric.Authorization.API.Logging.LogFactory;

namespace Fabric.Authorization.API
{
    public class Startup
    {
        private readonly string[] _allowedPaths =
        {
            "/swagger/ui/index",
            "/swagger/ui/index.html",
            "/swagger/ui/swagger.json",
            "/",
            $"/{AccessControl.Path}",
            $"/{AccessControl.Path}/index.html"
        };

        private readonly IAppConfiguration _appConfig;
        private readonly IdentityServerConfidentialClientSettings _idServerSettings;
        private readonly LoggingLevelSwitch _levelSwitch;
        private readonly ILogger _logger;
        private Bootstrapper _bootstrapper;

        public Startup(IHostingEnvironment env)
        {
            _appConfig =
                new AuthorizationConfigurationProvider(new WindowsCertificateService()).GetAppConfiguration(env
                    .ContentRootPath);

            _levelSwitch = new LoggingLevelSwitch();
            _idServerSettings = _appConfig.IdentityServerConfidentialClientSettings;
            _logger = LogFactory.CreateTraceLogger(_levelSwitch, _appConfig);
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

            _bootstrapper = new Bootstrapper(_logger, _appConfig, _levelSwitch, env, loggerFactory);

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
                .UseNancy(opt => opt.Bootstrapper = _bootstrapper);
        }

        public async Task<bool> HealthCheck()
        {
            var container = _bootstrapper.TinyIoCContainer;
            var clientStore = container.Resolve<IClientStore>();
            var results = await clientStore.GetAll();
            return results.Any();
        }
    }
}