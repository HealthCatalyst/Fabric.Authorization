using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using Fabric.Authorization.UnitTests.Mocks;
using Nancy;
using Nancy.Testing;

namespace Fabric.Authorization.UnitTests
{
    public abstract class ModuleTestsBase<T> where T: NancyModule
    {
        protected Browser CreateBrowser(params Claim[] claims)
        {
            return new Browser(CreateBootstrapper(claims), withDefaults => withDefaults.Accept("application/json"));
        }

        private ConfigurableBootstrapper CreateBootstrapper(params Claim[] claims)
        {
            var configurableBootstrapper = new ConfigurableBootstrapper();
            ConfigureBootstrapper(configurableBootstrapper, claims);
            return configurableBootstrapper;
        }

        protected virtual ConfigurableBootstrapper.ConfigurableBootstrapperConfigurator ConfigureBootstrapper(ConfigurableBootstrapper configurableBootstrapper, params Claim[] claims)
        {
            var configurableBootstrapperConfigurator = new ConfigurableBootstrapper.ConfigurableBootstrapperConfigurator(configurableBootstrapper);
            configurableBootstrapperConfigurator.Module<T>();
            configurableBootstrapperConfigurator.RequestStartup((container, pipeline, context) =>
            {
                context.CurrentUser = new TestPrincipal(claims);
            });
            return configurableBootstrapperConfigurator;
        }
    }
}
