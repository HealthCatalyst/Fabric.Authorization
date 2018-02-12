using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.API.RemoteServices.Identity.Providers;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Persistence.SqlServer.Configuration;
using Fabric.Platform.Shared.Configuration;
using Microsoft.AspNetCore.Hosting;
using Moq;
using Nancy;
using Nancy.Testing;
using Newtonsoft.Json;
using Serilog;
using Serilog.Core;
using Xunit;
using Xunit.Sdk;

namespace Fabric.Authorization.IntegrationTests
{
    public class IntegrationTestsFixture : IDisposable
    {
        protected ConnectionStrings ConnectionStrings { get; }
        private string DatabaseNameSuffix { get; }
        public IntegrationTestsFixture()
        {
            DatabaseNameSuffix = GetDatabaseNameSuffix();
            ConnectionStrings = GetSqlServerConnection(DatabaseNameSuffix);
            CreateSqlServerDatabase();
        }
        public Browser Browser { get; set; }

        public Browser GetBrowser(ClaimsPrincipal principal, string storageProvider, IIdentityServiceProvider identityServiceProvider = null)
        {            
            var appConfiguration = new AppConfiguration
            {
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
        
        private static readonly string SqlServerEnvironmentVariable = "SQLSERVERSETTINGS__SERVER";
        private static readonly string SqlServerUsernameEnvironmentVariable = "SQLSERVERSETTINGS__USERNAME";
        private static readonly string SqlServerPasswordEnvironmentVariable = "SQLSERVERSETTINGS__PASSWORD";

        public DefaultPropertySettings DefaultPropertySettings = new DefaultPropertySettings
        {
            GroupSource = "Windows"
        };
        
        
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

        #endregion IDisposable implementation

        public void CreateClient(Browser browser, string clientId)
        {
            var id = clientId;
            browser.Post("/clients", with =>
            {
                with.HttpRequest();
                with.JsonBody(new ClientApiModel { Id = id, Name = id });
            }).Wait();
        }

        public async Task AssociateUserToAdminRoleAsync(string user, string identityProvider, string storageProvider, string grain, string securableItem, string roleName)
        {
            var clientId = "fabric-installer";
            //var user = claims.First(c => c.Type == Claims.Sub).Value;
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(Claims.Scope, Scopes.ManageClientsScope),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.Scope, Scopes.ManageDosScope),
                new Claim(Claims.ClientId, clientId)
            }, "pwd"));

            var browser = GetBrowser(principal, storageProvider);

            var roleResponse = await browser.Get($"/roles/{grain}/{securableItem}/{roleName}", with =>
            {
                with.HttpRequest();
            });
            Assert.Equal(HttpStatusCode.OK, roleResponse.StatusCode);
            var role = JsonConvert.DeserializeObject<List<RoleApiModel>>(roleResponse.Body.AsString()).First();
            Assert.Equal(roleName, role.Name);

            var groupResponse = await browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = roleName + Guid.NewGuid(),
                    GroupSource = "Custom"
                });
            });
            Assert.Equal(HttpStatusCode.Created, groupResponse.StatusCode);
            var group = JsonConvert.DeserializeObject<GroupRoleApiModel>(groupResponse.Body.AsString());

            var groupRoleResponse = await browser.Post($"/groups/{group.GroupName}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    RoleId = role.Id
                });
            });
            Assert.Equal(HttpStatusCode.Created, groupRoleResponse.StatusCode);

            var groupUserResponse = await browser.Post($"/groups/{group.GroupName}/users", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = group.GroupName,
                    SubjectId = user,
                    IdentityProvider = identityProvider
                });
            });
            Assert.Equal(HttpStatusCode.Created, groupUserResponse.StatusCode);
        }

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