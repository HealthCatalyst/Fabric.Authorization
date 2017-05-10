using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Modules;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.UnitTests.Mocks;
using Moq;
using Nancy;
using Nancy.Testing;
using Serilog;
using Xunit;

namespace Fabric.Authorization.UnitTests.ClientsTests
{
    public class ClientsModuleTests
    {
        private readonly List<Client> _existingClients;
        private readonly Mock<IClientStore> _mockClientStore;
        private readonly Mock<ILogger> _mockLogger;

        public ClientsModuleTests()
        {
            _existingClients = new List<Client>
            {
                new Client
                {
                    Id = "sample-fabric-app",
                    Name = "Sample Fabric Client Application",
                    TopLevelSecurableItem = new SecurableItem
                    {
                        Id = Guid.NewGuid(),
                        Name = "sample-fabric-app"
                    }
                }
            };
           _mockClientStore = new Mock<IClientStore>().SetupGetClient(_existingClients);
            _mockLogger = new Mock<ILogger>();
        }

        [Fact]
        public void ClientsModuleTests_GetClients_ReturnsForbidden()
        {
            var clientsModule = CreateBrowser();
            var result = clientsModule.Get("/clients").Result;
            Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
        }

        [Fact]
        public void ClientsModuleTests_GetClient_ReturnsClient()
        {
            var existingClient = _existingClients.First();
            var clientsModule = CreateBrowser(new Claim(Claims.ClientId, existingClient.Id), new Claim(Claims.Scope, "fabric/authorization.read"));
            var result = clientsModule.Get($"/clients/{existingClient.Id}").Result;
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var clients = result.Body.DeserializeJson<Client>();
            Assert.Equal(existingClient.Id, clients.Id);
        }

        private Browser CreateBrowser(params Claim[] claims)
        {
            return new Browser(CreateBootstrapper(claims), withDefaults => withDefaults.Accept("application/json"));
        }

        private ConfigurableBootstrapper CreateBootstrapper(params Claim[] claims)
        {
            return new ConfigurableBootstrapper(with =>
            {
                with.Module<ClientsModule>()
                    .Dependency<IClientService>(typeof(ClientService))
                    .Dependency(_mockLogger.Object)
                    .Dependency(_mockClientStore.Object);

                with.RequestStartup((container, pipeline, context) =>
                {
                    context.CurrentUser = new TestPrincipal(claims);
                });
            });
        }
    }
}
