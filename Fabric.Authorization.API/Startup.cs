using System.Threading.Tasks;
using Fabric.Authorization.API.Configuration;
using Fabric.Platform.Auth;
using Fabric.Platform.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nancy.Owin;
using Serilog;
using Serilog.Core;

namespace Fabric.Authorization.API
{
    public class Startup
    {
        private readonly IConfiguration _config;
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .SetBasePath(env.ContentRootPath);

            _config = builder.Build();
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
            var appConfig = new AppConfiguration();
            ConfigurationBinder.Bind(_config, appConfig);
            var idServerSettings = appConfig.IdentityServerConfidentialClientSettings;

            var levelSwitch = new LoggingLevelSwitch();
            var logger = LogFactory.CreateLogger(levelSwitch, appConfig.ElasticSearchSettings, idServerSettings.ClientId);
            loggerFactory.AddSerilog(logger);

            logger.Information("Configuration Settings: {@appConfig}", appConfig);

            app.UseIdentityServerAuthentication(new IdentityServerAuthenticationOptions
            {
                Authority = idServerSettings.Authority,
                RequireHttpsMetadata = false,

                ApiName = idServerSettings.ClientId
            });
            app.UseOwin()
                .UseFabricLoggingAndMonitoring(logger, () => Task.FromResult(true), levelSwitch)
                .UseAuthPlatform(idServerSettings.Scopes)
                .UseNancy(opt => opt.Bootstrapper = new Bootstrapper(logger, appConfig));
        }
    }
}
