using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Catalyst.Fabric.Authorization.Models;
using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.RemoteServices.Identity.Providers;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Persistence.SqlServer.Configuration;
using Fabric.Authorization.Persistence.SqlServer.Services;
using Fabric.Authorization.Persistence.SqlServer.Stores.EDW;
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
        public ConnectionStrings ConnectionStrings { get; set; }
        private string DatabaseNameSuffix { get; }
        public IntegrationTestsFixture()
        {
            DatabaseNameSuffix = GetDatabaseNameSuffix();
            ConnectionStrings = GetSqlServerConnection(DatabaseNameSuffix);
        }
        public Browser Browser { get; set; }

        public string TestHost => "http://testhost:80/v1";

        public ISecurityContext GetEdwAdminContext(string storageProvider)
        {
            if(storageProvider == StorageProviders.InMemory)
            {
                return new InMemorySecurityContext(this.ConnectionStrings);
            }
            else if(storageProvider == StorageProviders.SqlServer)
            {
                return new SecurityContext(this.ConnectionStrings);
            }
            else
            {
                throw new NotImplementedException("Database not supported");
            }
        }

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
                    GroupSource = "Windows",
                    DualStoreEDWAdminPermissions = true
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

        public DefaultPropertySettings DefaultPropertySettings = new DefaultPropertySettings
        {
            GroupSource = "Windows",
            DualStoreEDWAdminPermissions = true
        };
        
        
        private string GetDatabaseNameSuffix()
        {
            var id = Guid.NewGuid().ToString().Replace("-", "");
            return id;
        }

        private ConnectionStrings GetSqlServerConnection(string databaseNameSuffix)
        {
            var connectionString = new ConnectionStrings
            {
                AuthorizationDatabase = $"Authorization-{databaseNameSuffix}",
                 EDWAdminDatabase = $"EDWAdmin-{databaseNameSuffix}"
            };

            return connectionString;
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
            Task.Run(async () => await browser.Post("/clients", with =>
            {
                with.HttpRequest();
                with.JsonBody(new ClientApiModel { Id = id, Name = id });
            })).Wait();
        }

        public async Task AssociateUserToAdminRoleAsync(string user, string identityProvider, string storageProvider, string grain, string securableItem, string roleName)
        {
            var clientId = Domain.Defaults.Authorization.InstallerClientId;

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
                with.JsonBody(new[]
                {
                    new
                    {
                        role.Grain,
                        role.SecurableItem,
                        role.Name,
                        role.Id
                    }
                });
            });
            Assert.Equal(HttpStatusCode.OK, groupRoleResponse.StatusCode);

            var groupUserResponse = await browser.Post($"/groups/{group.GroupName}/users", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new
                    {
                        group.GroupName,
                        SubjectId = user,
                        IdentityProvider = identityProvider
                    }
                });
            });
            Assert.Equal(HttpStatusCode.OK, groupUserResponse.StatusCode);
        }

        public class DisplayTestMethodNameAttribute : BeforeAfterTestAttribute
        {
            private bool _writeToConsole = true;
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