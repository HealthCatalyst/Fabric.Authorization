using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Models;
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

namespace Fabric.Authorization.UnitTests.SecurableItems
{
    public class SecurableItemsModuleTests : ModuleTestsBase<SecurableItemsModule>
    {
        private readonly List<Client> _existingClients;
        private readonly Mock<IClientStore> _mockClientStore;
        private readonly Mock<ILogger> _mockLogger;

        public SecurableItemsModuleTests()
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

            _mockClientStore = new Mock<IClientStore>()
                .SetupGetClient(_existingClients)
                .SetupAddClient();

            _mockLogger = new Mock<ILogger>();
        }

        [Fact]
        public void GetSecurableItem_ReturnsTopLevelItem()
        {
            var existingClient = _existingClients.First();
            var securableItemsModule = CreateBrowser(new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.ClientId, existingClient.Id));
            var result = securableItemsModule.Get("/securableitems").Result;
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var secureableItem = result.Body.DeserializeJson<SecurableItemApiModel>();
            Assert.Equal(existingClient.TopLevelSecurableItem.Id, secureableItem.Id);
        }

        protected override ConfigurableBootstrapper.ConfigurableBootstrapperConfigurator ConfigureBootstrapper(ConfigurableBootstrapper configurableBootstrapper,
            params Claim[] claims)
        {
            return base.ConfigureBootstrapper(configurableBootstrapper, claims)
                .Dependency<ISecurableItemService>(typeof(SecurableItemService))
                .Dependency(_mockClientStore.Object)
                .Dependency(_mockLogger.Object);
        }
    }
}
