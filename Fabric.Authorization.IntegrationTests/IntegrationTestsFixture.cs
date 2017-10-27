using System;
using System.Reflection;
using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.API.Services;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Domain.Stores.CouchDB;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Nancy.Testing;
using Serilog;
using Xunit.Sdk;

namespace Fabric.Authorization.IntegrationTests
{
    public class IntegrationTestsFixture : IDisposable
    {
        public Browser Browser { get; set; }

        public ILogger Logger { get; set; } = new Mock<ILogger>().Object;

        public IEventContextResolverService EventContextResolverService { get; set; } =
            new Mock<IEventContextResolverService>().Object;

        private IDocumentDbService _dbService;

        private readonly string CouchDbServerEnvironmentVariable = "COUCHDBSETTINGS__SERVER";

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

            ICouchDbSettings config = new CouchDbSettings()
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