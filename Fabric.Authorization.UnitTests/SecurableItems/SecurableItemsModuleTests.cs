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

        [Fact]
        public void AddSecurableItemByClientId_Successful()
        {
            var existingClient = _existingClients.First();
            var securableItemsModule = CreateBrowser(new Claim(Claims.ClientId, existingClient.Id),
                new Claim(Claims.Scope, Scopes.WriteScope));
            var securableItemToPost = new SecurableItemApiModel
            {
                Name = "inner-securable-3"
            };
            var result = securableItemsModule.Post("/securableitems", with => with.JsonBody(securableItemToPost))
                .Result;
            Assert.Equal(HttpStatusCode.Created, result.StatusCode);
            var newSecurableItem = result.Body.DeserializeJson<SecurableItemApiModel>();
            Assert.Equal(securableItemToPost.Name, newSecurableItem.Name);
            Assert.NotNull(newSecurableItem.Id);
        }

        [Theory, MemberData(nameof(BadRequestData))]
        public void AddSecurableItem_BadRequest(SecurableItemApiModel securableItemToPost, int errorCount, bool itemLevel)
        {
            var existingClient = _existingClients.First();
            var innerSecurable1 = existingClient.TopLevelSecurableItem.SecurableItems.First(s => s.Name == "inner-securable-1");
            var securableItemsModule = CreateBrowser(new Claim(Claims.ClientId, existingClient.Id),
                new Claim(Claims.Scope, Scopes.WriteScope));
            var requestUrl = "/securableitems";
            if (itemLevel)
            {
                requestUrl = requestUrl + $"/{innerSecurable1.Id}";
            }
            var result = securableItemsModule.Post(requestUrl, with => with.JsonBody(securableItemToPost))
                .Result;
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            var error = result.Body.DeserializeJson<Error>();
            Assert.NotNull(error);
            if (errorCount > 0)
            {
                Assert.Equal(errorCount, error.Details.Length);
            }
        }

        [Theory, MemberData(nameof(BadScopes))]
        public void AddSecurableItem_ReturnsForbidden(Claim scopeClaim, Claim clientIdClaim, bool itemLevel)
        {
            var existingClient = _existingClients.First();
            var innerSecurable1 = existingClient.TopLevelSecurableItem.SecurableItems.First(s => s.Name == "inner-securable-1");
            if (clientIdClaim != null && clientIdClaim.Value =="valid")
            {
                clientIdClaim = new Claim(Claims.ClientId, existingClient.Id);
            }
            var securableItemToPost = new SecurableItemApiModel
            {
                Name = "inner-securable-3"
            };
            var securableItemsModule = CreateBrowser(scopeClaim, clientIdClaim);
            var requestUrl = "/securableitems";
            if (itemLevel)
            {
                requestUrl = requestUrl + $"/{innerSecurable1.Id}";
            }
            var result = securableItemsModule.Post(requestUrl, with => with.JsonBody(securableItemToPost))
                .Result;
            Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
        }

        [Fact]
        public void AddSecurableItemByItemId_Successful()
        {
            //Arrange
            var existingClient = _existingClients.First();
            var innerSecurable2 = existingClient.TopLevelSecurableItem.SecurableItems.First(s => s.Name == "inner-securable-2");
            var securableItemsModule = CreateBrowser(new Claim(Claims.ClientId, existingClient.Id),
                new Claim(Claims.Scope, Scopes.WriteScope), new Claim(Claims.Scope, Scopes.ReadScope));
            var securableItemToPost = new SecurableItemApiModel
            {
                Name = "inner-securable-3"
            };

            //Act
            var result = securableItemsModule.Post($"/securableitems/{innerSecurable2.Id}", with => with.JsonBody(securableItemToPost))
                .Result;

            //Assert
            //Ensure response is correct
            Assert.Equal(HttpStatusCode.Created, result.StatusCode);
            var newSecurableItem = result.Body.DeserializeJson<SecurableItemApiModel>();
            Assert.Equal(securableItemToPost.Name, newSecurableItem.Name);
            Assert.NotNull(newSecurableItem.Id);

            //Get the whole hierarchy to ensure that the new item is in the expected location
            var getResult = securableItemsModule.Get("/securableitems").Result;
            Assert.Equal(HttpStatusCode.OK, getResult.StatusCode);
            var securableItemHierarchy = getResult.Body.DeserializeJson<SecurableItemApiModel>();
            newSecurableItem = securableItemHierarchy.SecurableItems.First(s => s.Name == "inner-securable-2")
                .SecurableItems.First(s => s.Name == securableItemToPost.Name);
            Assert.NotNull(newSecurableItem);
        }

        
        public static IEnumerable<object[]> BadRequestData => new[]
        {
            new object[] { new SecurableItemApiModel{ Name = null}, 1, false},
            new object[] { new SecurableItemApiModel{ Name = string.Empty}, 1, false},
            new object[] { new SecurableItemApiModel{ Name = "inner-securable-1" }, 0, false},
            new object[] { new SecurableItemApiModel{ Name = null}, 1, true},
            new object[] { new SecurableItemApiModel{ Name = string.Empty}, 1, true},
            new object[] { new SecurableItemApiModel{ Name = "inner-securable-1" }, 0, true}
        };

        public static IEnumerable<object[]> BadScopes => new[]
        {
            new object[] { new Claim(Claims.Scope, Scopes.WriteScope), new Claim(Claims.ClientId, "invalid"), false},
            new object[] { new Claim(Claims.Scope, Scopes.ReadScope), new Claim(Claims.ClientId, "valid"), false},
            new object[] { new Claim(Claims.Scope, Scopes.WriteScope), null, false},
            new object[] { null, new Claim(Claims.ClientId, "valid"), false},
            new object[] { new Claim(Claims.Scope, Scopes.WriteScope), new Claim(Claims.ClientId, "invalid"), true},
            new object[] { new Claim(Claims.Scope, Scopes.ReadScope), new Claim(Claims.ClientId, "valid"), true},
            new object[] { new Claim(Claims.Scope, Scopes.WriteScope), null, true},
            new object[] { null, new Claim(Claims.ClientId, "valid"), true},
        };

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
