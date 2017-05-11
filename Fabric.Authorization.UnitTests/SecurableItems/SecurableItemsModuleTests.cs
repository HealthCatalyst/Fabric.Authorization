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
                        Name = "sample-fabric-app",
                        SecurableItems = new List<SecurableItem>
                        {
                            new SecurableItem
                            {
                                Id = Guid.NewGuid(),
                                Name = "inner-securable-1"
                            },
                            new SecurableItem
                            {
                                Id = Guid.NewGuid(),
                                Name = "inner-securable-2"
                            }
                        }
                    }
                }
            };

            _mockClientStore = new Mock<IClientStore>()
                .SetupGetClient(_existingClients)
                .SetupAddClient();

            _mockLogger = new Mock<ILogger>();
        }

        [Fact]
        public void GetSecurableItemByClientId_ReturnsTopLevelItem()
        {
            var existingClient = _existingClients.First();
            var securableItemsModule = CreateBrowser(new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.ClientId, existingClient.Id));
            var result = securableItemsModule.Get("/securableitems").Result;
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var secureableItem = result.Body.DeserializeJson<SecurableItemApiModel>();
            Assert.Equal(existingClient.TopLevelSecurableItem.Id, secureableItem.Id);
        }

        [Fact]
        public void GetSecurableItemByClientId_ReturnsForbidden()
        {
            var existingClient = _existingClients.First();
            var securableItemsModule = CreateBrowser(new Claim(Claims.ClientId, existingClient.Id));
            var result = securableItemsModule.Get("/securableitems").Result;
            Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
        }

        [Fact]
        public void GetSecurableItemById_ReturnsItem()
        {
            var existingClient = _existingClients.First();
            var innerSecurable = existingClient.TopLevelSecurableItem.SecurableItems.First();
            var securableItemsModule = CreateBrowser(new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.ClientId, existingClient.Id));
            var result = securableItemsModule.Get($"/securableitems/{innerSecurable.Id}").Result;
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var secureableItem = result.Body.DeserializeJson<SecurableItemApiModel>();
            Assert.Equal(innerSecurable.Id, secureableItem.Id);
        }

        [Fact]
        public void GetSecurableItemByItemId_ReturnsForbidden()
        {
            var existingClient = _existingClients.First();
            var innerSecurable = existingClient.TopLevelSecurableItem.SecurableItems.First();
            var securableItemsModule = CreateBrowser(new Claim(Claims.ClientId, existingClient.Id));
            var result = securableItemsModule.Get($"/securableitems/{innerSecurable.Id}").Result;
            Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
        }

        [Fact]
        public void GetSecurableItemByItemId_ReturnsError_WithInvalidClientId()
        {
            var existingClient = _existingClients.First();
            var innerSecurable = existingClient.TopLevelSecurableItem.SecurableItems.First();
            var securableItemsModule = CreateBrowser(new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.ClientId, "nonexistentId"));
            var result = securableItemsModule.Get($"/securableitems/{innerSecurable.Id}").Result;
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Fact]
        public void GetSecurableItemByItemId_ReturnsError_WithInvalidItemId()
        {
            var existingClient = _existingClients.First();
            var securableItemsModule = CreateBrowser(new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.ClientId, existingClient.Id));
            var result = securableItemsModule.Get($"/securableitems/{Guid.NewGuid()}").Result;
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
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
