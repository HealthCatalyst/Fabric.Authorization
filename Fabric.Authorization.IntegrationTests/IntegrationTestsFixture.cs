using System;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Security.Claims;
using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.API.RemoteServices.Identity.Providers;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Persistence.CouchDb.Configuration;
using Fabric.Authorization.Persistence.CouchDb.Services;
using Fabric.Authorization.Persistence.CouchDb.Stores;
using Fabric.Authorization.Persistence.SqlServer.Configuration;
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
        protected CouchDbSettings CouchDbSettings { get; }
        protected ConnectionStrings ConnectionStrings { get; }
        private string DatabaseNameSuffix { get; }
        public IntegrationTestsFixture()
        {
            CouchDbSettings = GetCouchDbSettings();
            DatabaseNameSuffix = GetDatabaseNameSuffix();
            ConnectionStrings = GetSqlServerConnection(DatabaseNameSuffix);
            CreateSqlServerDatabase();
        }
        public Browser Browser { get; set; }

        public Browser GetBrowser(ClaimsPrincipal principal, string storageProvider, IIdentityServiceProvider identityServiceProvider = null)
        {
            var appConfiguration = new AppConfiguration
            {
                CouchDbSettings = CouchDbSettings,
                StorageProvider = storageProvider,
                ConnectionStrings = ConnectionStrings,
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
                }
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
        private static readonly string SqlServerEnvironmentVariable = "SQLSERVERSETTINGS__SERVER";
        private static readonly string SqlServerUsernameEnvironmentVariable = "SQLSERVERSETTINGS__USERNAME";
        private static readonly string SqlServerPasswordEnvironmentVariable = "SQLSERVERSETTINGS__PASSWORD";

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

        private CouchDbSettings GetCouchDbSettings()
        {
            var databaseNameSuffix = GetDatabaseNameSuffix();
            CouchDbSettings config = new CouchDbSettings
            {
                DatabaseName = "integration-" + databaseNameSuffix,
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

        private string GetDatabaseNameSuffix()
        {
            var id = Guid.NewGuid().ToString().Replace("-", "");
            return id;
        }

        private ConnectionStrings GetSqlServerConnection(string databaseNameSuffix)
        {
            var sqlServerHost = Environment.GetEnvironmentVariable(SqlServerEnvironmentVariable) ?? ".";
            var sqlServerSecurityString = GetSqlServerSecurityString();
            var connectionString = new ConnectionStrings
            {
                AuthorizationDatabase = $"Server={sqlServerHost};Database=Authorization-{databaseNameSuffix};{sqlServerSecurityString};MultipleActiveResultSets=true"
            };

            return connectionString;
        }

        private string GetSqlServerSecurityString()
        {
            var sqlServerUserName = Environment.GetEnvironmentVariable(SqlServerUsernameEnvironmentVariable);
            var sqlServerPassword = Environment.GetEnvironmentVariable(SqlServerPasswordEnvironmentVariable);
            var securityString = "Trusted_Connection=True";
            if (!string.IsNullOrEmpty(sqlServerUserName) && !string.IsNullOrEmpty(sqlServerPassword))
            {
                securityString = $"User Id={sqlServerUserName};Password={sqlServerPassword}";
            }
            return securityString;
        }

        private void CreateSqlServerDatabase()
        {
            var targetDbName = $"Authorization-{DatabaseNameSuffix}";
            Console.WriteLine($"creating database: {targetDbName}");
            var connection =
                ConnectionStrings.AuthorizationDatabase.Replace(targetDbName, "master");
            var file = new FileInfo("Fabric.Authorization.SqlServer_Create.sql");

            var createDbScript = file.OpenText().ReadToEnd()
                .Replace("$(DatabaseName)", targetDbName);

            var splitter = new[] { "GO\r\n" };
            var commandTexts = createDbScript.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
            int x;
            using (var conn = new SqlConnection(connection))
            {
                conn.Open();
                using (var command = new SqlCommand("query", conn))
                {
                    for (x = 0; x < commandTexts.Length; x++)
                    {
                        var commandText = commandTexts[x];

                        // break if we just created the Identity DB
                        if (commandText.StartsWith("CREATE DATABASE"))
                        {
                            var commandParts = commandText.Split(
                                new[] { " ON " },
                                StringSplitOptions.RemoveEmptyEntries);

                            command.CommandText = commandParts[0];
                            command.ExecuteNonQuery();
                            break;
                        }
                    }
                }
            }

            // establish a connection to the newly created Identity DB
            using (var conn = new SqlConnection(ConnectionStrings.AuthorizationDatabase))
            {
                conn.Open();

                using (var command = new SqlCommand("query", conn))
                {
                    for (x = x + 1; x < commandTexts.Length; x++)
                    {
                        var commandText = commandTexts[x];

                        // skip generated SqlPackage commands and comments
                        if (commandText.StartsWith(":") || commandText.StartsWith("/*"))
                        {
                            continue;
                        }

                        command.CommandText = commandText.Replace("HCFabricAuthorizationData1", "PRIMARY").Replace("HCFabricAuthorizationIndex1", "PRIMARY").TrimEnd(Environment.NewLine.ToCharArray());
                        command.ExecuteNonQuery();
                    }
                }
            }
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