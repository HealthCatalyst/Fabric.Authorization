using System;
using System.Reflection;
using System.Security.Claims;
using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.API.Services;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Domain.Stores.CouchDB;
using Fabric.Platform.Shared.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Nancy.Testing;
using Serilog;
using Serilog.Core;
using Xunit.Sdk;

namespace Fabric.Authorization.IntegrationTests
{
    public class IntegrationTestsFixture : IDisposable
    {
        public Browser Browser { get; set; }

        public Browser GetBrowser(ClaimsPrincipal principal, bool useInMemoryStores)
        {
            var appConfiguration = new AppConfiguration
            {
                CouchDbSettings = GetCouchDbSettings(),
                UseInMemoryStores = useInMemoryStores,
                IdentityServerConfidentialClientSettings = new IdentityServerConfidentialClientSettings
                {
                    Authority = "http://localhost",
                    ClientId = "test",
                    ClientSecret = "test",
                    Scopes = new[]
                    {
                        "fabric/authorization.read",
                        "fabric/authorization.write",
                        "fabric/authorization.manageclients"
                    }
                }
            };
            var hostingEnvironment = new Mock<IHostingEnvironment>();
            var bootstrapper = new TestBootstrapper(new Mock<ILogger>().Object, appConfiguration,
                new LoggingLevelSwitch(), hostingEnvironment.Object, principal);
            return new Browser(bootstrapper, context =>
            {
                context.HostName("testhost");
                context.Header("Content-Type", "application/json");
                context.Header("Accept", "application/json");
            });
        }

        public ILogger Logger { get; set; } = new Mock<ILogger>().Object;

        public IEventContextResolverService EventContextResolverService { get; set; } =
            new Mock<IEventContextResolverService>().Object;

        private IDocumentDbService _dbService;

        private readonly string CouchDbServerEnvironmentVariable = "COUCHDBSETTINGS__SERVER";
        private readonly string CouchDbUsernameEnvironmentVariable = "COUCHDBSETTINGS__USERNAME";
        private readonly string CouchDbPasswordEnvironmentVariable = "COUCHDBSETTINGS__PASSWORD";

        protected DefaultPropertySettings DefaultPropertySettings = new DefaultPropertySettings
        {
            GroupSource = "Windows"
        };

        protected IDocumentDbService DbService()
        {
            if (_dbService != null)
            {
                return _dbService;
            }

            ICouchDbSettings config = GetCouchDbSettings();

            var innerDbService = new CouchDbAccessService(config, new Mock<ILogger>().Object);
            innerDbService.Initialize().Wait();
            innerDbService.AddViews("roles", CouchDbRoleStore.GetViews()).Wait();
            innerDbService.AddViews("permissions", CouchDbPermissionStore.GetViews()).Wait();
            var auditingDbService = new AuditingDocumentDbService(new Mock<IEventService>().Object, innerDbService);
            var cachingDbService =
                new CachingDocumentDbService(auditingDbService, new MemoryCache(new MemoryCacheOptions()));
            _dbService = cachingDbService;
            return _dbService;
        }

        private CouchDbSettings GetCouchDbSettings()
        {
            CouchDbSettings config = new CouchDbSettings()
            {
                DatabaseName = "integration-" + DateTime.UtcNow.Ticks,
                Username = "",
                Password = "",
                Server = "http://127.0.0.1:5984"
            };

            var couchDbServer = Environment.GetEnvironmentVariable(CouchDbServerEnvironmentVariable);
            if (!string.IsNullOrEmpty(couchDbServer))
            {
                config.Server = couchDbServer;
            }

            var couchDbUsername = Environment.GetEnvironmentVariable(CouchDbUsernameEnvironmentVariable);
            if (!string.IsNullOrEmpty(couchDbUsername))
            {
                config.Username = couchDbUsername;
            }

            var couchDbPassword = Environment.GetEnvironmentVariable(CouchDbPasswordEnvironmentVariable);
            if (!string.IsNullOrEmpty(couchDbPassword))
            {
                config.Password = couchDbPassword;
            }
            return config;
        }

        #region IDisposable implementation

        // Dispose() calls Dispose(true)
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~IntegrationTestsFixture()
        {
            // Finalizer calls Dispose(false)
            Dispose(false);
        }

        // The bulk of the clean-up code is implemented in Dispose(bool)
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                //this.Browser = null;
            }
        }

        #endregion IDisposable implementation

        protected class DisplayTestMethodNameAttribute : BeforeAfterTestAttribute
        {
            private bool _writeToConsole = false;
            public override void Before(MethodInfo methodUnderTest)
            {
                if (_writeToConsole)
                {
                    Console.WriteLine(
                        $"Running test '{methodUnderTest.DeclaringType.Name}.{methodUnderTest.Name}'");
                }
                base.Before(methodUnderTest);
            }

            public override void After(MethodInfo methodUnderTest)
            {
                if (_writeToConsole)
                {
                    Console.WriteLine(
                        $"Finished test '{methodUnderTest.DeclaringType.Name}.{methodUnderTest.Name}.'");
                }
                base.After(methodUnderTest);
            }
        }
    }
}