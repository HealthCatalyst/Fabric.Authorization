using System;
using System.Reflection;
using System.Security.Claims;
using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.API.Infrastructure;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.API.RemoteServices.Identity.Providers;
using Fabric.Authorization.API.Services;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Persistence.CouchDb.Configuration;
using Fabric.Authorization.Persistence.CouchDb.Services;
using Fabric.Authorization.Persistence.CouchDb.Stores;
using Fabric.Authorization.Persistence.SqlServer.Configuration;
using Fabric.Authorization.Persistence.SqlServer.Services;
using Fabric.Platform.Shared.Configuration;
using IdentityModel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Nancy;
using Nancy.Testing;
using Serilog;
using Serilog.Core;
using Xunit.Sdk;

namespace Fabric.Authorization.IntegrationTests
{
    public class IntegrationTestsFixture : IDisposable
    {
        protected CouchDbSettings CouchDbSettings { get; }
        private static ConnectionStrings _connectionStrings;

        public IntegrationTestsFixture()
        {
            CouchDbSettings = GetCouchDbSettings();
        }
        public Browser Browser { get; set; }

        public Browser GetBrowser(ClaimsPrincipal principal, string storageProvider, IIdentityServiceProvider identityServiceProvider = null)
        {
            var appConfiguration = new AppConfiguration
            {
                CouchDbSettings = CouchDbSettings,
                StorageProvider = storageProvider,
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
                },
                DefaultPropertySettings = new DefaultPropertySettings
                {
                    GroupSource = "Windows"
                },
                ConnectionStrings = ConnectionStrings
            };
            var hostingEnvironment = new Mock<IHostingEnvironment>();
            
            var bootstrapper = new TestBootstrapper(new Mock<ILogger>().Object, appConfiguration,
                new LoggingLevelSwitch(), hostingEnvironment.Object, principal, identityServiceProvider);

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

        public DefaultPropertySettings DefaultPropertySettings = new DefaultPropertySettings
        {
            GroupSource = "Windows"
        };

        public IDocumentDbService DbService()
        {
            if (_dbService != null)
            {
                return _dbService;
            }

            ICouchDbSettings config = CouchDbSettings;

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

        private static ConnectionStrings ConnectionStrings
        {
            get
            {
                if (_connectionStrings != null) return _connectionStrings;
                _connectionStrings = new ConnectionStrings
                {
                    AuthorizationDatabase = 
                        $"Server=.;Database=Authorization;TrustedConnection=true;MultipleActiveResultSets=true"
                };
                Console.WriteLine($"Connection String for tests: {_connectionStrings.AuthorizationDatabase}");
                return _connectionStrings;
            }
        }

        protected static AuthorizationDbContext IdentityDbContext
        {
            get
            {         
                var builder = new DbContextOptionsBuilder<AuthorizationDbContext>();

                builder.UseSqlServer(ConnectionStrings.AuthorizationDatabase);

                var testIdentity = new ClaimsIdentity();
                testIdentity.AddClaim(new Claim(JwtClaimTypes.ClientId, "testing"));
                
                var nancyContext = new NancyContext {CurrentUser = new ClaimsPrincipal(testIdentity)};
                var nancyContextWrapper = new NancyContextWrapper(nancyContext);

                return new AuthorizationDbContext(builder.Options, new EventContextResolverService(nancyContextWrapper));
            }
        }

        private CouchDbSettings GetCouchDbSettings()
        {
            CouchDbSettings config = new CouchDbSettings
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

        public void CreateClient(Browser browser, string clientId)
        {
            var id = clientId;
            browser.Post("/clients", with =>
            {
                with.HttpRequest();
                with.JsonBody(new ClientApiModel { Id = id, Name = id });
            }).Wait();
        }

        #endregion IDisposable implementation

        public class DisplayTestMethodNameAttribute : BeforeAfterTestAttribute
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