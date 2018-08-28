using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Catalyst.Fabric.Authorization.Models;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Modules;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Resolvers.Permissions;
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
        private readonly Mock<ISecurableItemStore> _mockSecurableItemStore;
        private readonly Mock<ILogger> _mockLogger;

        private const string FabricSampleAppClientId = "sample-fabric-app";
        private const string DosClientId = "dos-metadata-service";

        public SecurableItemsModuleTests()
        {
            _existingClients = new List<Client>
            {
                new Client
                {
                    Id = FabricSampleAppClientId,
                    Name = "Sample Fabric Client Application",
                    TopLevelSecurableItem = new SecurableItem
                    {
                        Id = Guid.NewGuid(),
                        Grain = Domain.Defaults.Authorization.AppGrain,
                        Name = FabricSampleAppClientId,
                        ClientOwner = FabricSampleAppClientId,
                        SecurableItems = new List<SecurableItem>
                        {
                            new SecurableItem
                            {
                                Id = Guid.NewGuid(),
                                Grain = Domain.Defaults.Authorization.AppGrain,
                                ClientOwner = FabricSampleAppClientId,
                                Name = "inner-securable-1"
                            },
                            new SecurableItem
                            {
                                Id = Guid.NewGuid(),
                                Grain = Domain.Defaults.Authorization.AppGrain,
                                ClientOwner = FabricSampleAppClientId,
                                Name = "inner-securable-2"
                            }
                        }
                    }
                },
                new Client
                {
                    Id = DosClientId,
                    Name = "DOS Metadata Service",
                    TopLevelSecurableItem = new SecurableItem
                    {
                        Id = Guid.NewGuid(),
                        Grain = Domain.Defaults.Authorization.AppGrain,
                        Name = DosClientId,
                        ClientOwner = DosClientId,
                        SecurableItems = new List<SecurableItem>
                        {
                            new SecurableItem
                            {
                                Id = Guid.NewGuid(),
                                Grain = Domain.Defaults.Authorization.AppGrain,
                                ClientOwner = "dos-metadata-service",
                                Name = "dos-top-level-app-sec-item"
                            },
                        }
                    }
                }
            };

            var dosSecurableItems = new List<SecurableItem>
            {
                new SecurableItem
                {
                    Id = Guid.NewGuid(),
                    Grain = Domain.Defaults.Authorization.DosGrain,
                    ClientOwner = DosClientId,
                    Name = "datamarts"
                }
            };

            _mockClientStore = new Mock<IClientStore>()
                .SetupGetClient(_existingClients)
                .SetupAddClient();

            var secItems = _existingClients
                .Select(c => c.TopLevelSecurableItem)
                .Union(_existingClients.SelectMany(c => c.TopLevelSecurableItem.SecurableItems))
                .Union(dosSecurableItems);

            _mockSecurableItemStore = new Mock<ISecurableItemStore>()
                .SetupGetSecurableItem(secItems.ToList());

            _mockLogger = new Mock<ILogger>();

            MockGrainStore.SetupMock(new List<Grain>
            {
                new Grain
                {
                    Id = Guid.NewGuid(),
                    Name = Domain.Defaults.Authorization.AppGrain
                },
                new Grain
                {
                    Id = Guid.NewGuid(),
                    Name = Domain.Defaults.Authorization.DosGrain,
                    RequiredWriteScopes = new List<string> {"fabric/authorization.dos.write"},
                    IsShared = true
                }
            });
        }

        [Fact]
        public void GetSecurableItemByClientId_ReturnsTopLevelItem()
        {
            var existingClient = _existingClients.First(c => c.Id == FabricSampleAppClientId);
            var securableItemsModule = CreateBrowser(new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.ClientId, existingClient.Id));
            var result = securableItemsModule.Get("/securableitems").Result;
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var securableItem = result.Body.DeserializeJson<SecurableItemApiModel>();
            Assert.Equal(existingClient.TopLevelSecurableItem.Id, securableItem.Id);
        }

        [Fact]
        public void GetSecurableItemByClientId_ReturnsForbidden()
        {
            var existingClient = _existingClients.First(c => c.Id == FabricSampleAppClientId);
            var securableItemsModule = CreateBrowser(new Claim(Claims.ClientId, existingClient.Id));
            var result = securableItemsModule.Get("/securableitems").Result;
            Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
        }

        [Fact]
        public void GetSecurableItemById_ReturnsItem()
        {
            var existingClient = _existingClients.First(c => c.Id == FabricSampleAppClientId);
            var innerSecurable = existingClient.TopLevelSecurableItem.SecurableItems.First();
            var securableItemsModule = CreateBrowser(new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.ClientId, existingClient.Id));
            var result = securableItemsModule.Get($"/securableitems/{innerSecurable.Id}").Result;
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var securableItem = result.Body.DeserializeJson<SecurableItemApiModel>();
            Assert.Equal(innerSecurable.Id, securableItem.Id);
        }

        [Fact]
        public void GetSecurableItemByItemId_ReturnsForbidden()
        {
            var existingClient = _existingClients.First(c => c.Id == FabricSampleAppClientId);
            var innerSecurable = existingClient.TopLevelSecurableItem.SecurableItems.First();
            var securableItemsModule = CreateBrowser(new Claim(Claims.ClientId, existingClient.Id));
            var result = securableItemsModule.Get($"/securableitems/{innerSecurable.Id}").Result;
            Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
        }

        [Fact]
        public void GetSecurableItemByItemId_ReturnsError_WithInvalidClientId()
        {
            var existingClient = _existingClients.First(c => c.Id == FabricSampleAppClientId);
            var innerSecurable = existingClient.TopLevelSecurableItem.SecurableItems.First();
            var securableItemsModule = CreateBrowser(new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.ClientId, "nonexistentId"));
            var result = securableItemsModule.Get($"/securableitems/{innerSecurable.Id}").Result;
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        }

        [Fact]
        public void GetSecurableItemByItemId_ReturnsError_WithInvalidItemId()
        {
            var existingClient = _existingClients.First(c => c.Id == FabricSampleAppClientId);
            var securableItemsModule = CreateBrowser(new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.ClientId, existingClient.Id));
            var result = securableItemsModule.Get($"/securableitems/{Guid.NewGuid()}").Result;
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        }

        [Theory, MemberData(nameof(BadRequestData))]
        public void AddSecurableItem_BadRequest(SecurableItemApiModel securableItemToPost, int errorCount,
            bool itemLevel)
        {
            var existingClient = _existingClients.First(c => c.Id == FabricSampleAppClientId);
            var securableItemsModule = CreateBrowser(new Claim(Claims.ClientId, existingClient.Id),
                new Claim(Claims.Scope, Scopes.WriteScope));
            var requestUrl = "/securableitems";
            if (itemLevel)
            {
                var innerSecurable1 =
                    existingClient.TopLevelSecurableItem.SecurableItems.First(s => s.Name == "inner-securable-1");
                requestUrl = $"{requestUrl}/{innerSecurable1.Id}";
            }
            var result = securableItemsModule.Post(requestUrl, with => with.JsonBody(securableItemToPost)).Result;
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
            var existingClient = _existingClients.First(c => c.Id == FabricSampleAppClientId);
            var requestClientIdClaim = (clientIdClaim != null && clientIdClaim.Value == "valid")
                ? new Claim(Claims.ClientId, existingClient.Id)
                : clientIdClaim;
            var securableItemToPost = new SecurableItemApiModel
            {
                ClientOwner = FabricSampleAppClientId,
                Name = "inner-securable-3"
            };
            var securableItemsModule = CreateBrowser(scopeClaim, requestClientIdClaim);
            var requestUrl = "/securableitems";
            if (itemLevel)
            {
                var innerSecurable1 =
                    existingClient.TopLevelSecurableItem.SecurableItems.First(s => s.Name == "inner-securable-1");
                requestUrl = requestUrl + $"/{innerSecurable1.Id}";
            }
            var result = securableItemsModule.Post(requestUrl, with => with.JsonBody(securableItemToPost))
                .Result;
            Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
        }

        [Theory, MemberData(nameof(ConflictData))]
        public void AddSecurableItem_Conflict(SecurableItemApiModel securableItemToPost, bool itemLevel)
        {
            var existingClient = _existingClients.First(c => c.Id == FabricSampleAppClientId);
            var innerSecurable1 =
                existingClient.TopLevelSecurableItem.SecurableItems.First(s => s.Name == "inner-securable-1");
            var securableItemsModule = CreateBrowser(new Claim(Claims.ClientId, existingClient.Id),
                new Claim(Claims.Scope, Scopes.WriteScope));
            var requestUrl = "/securableitems";
            if (itemLevel)
            {
                requestUrl = $"{requestUrl}/{innerSecurable1.Id}";
            }
            var result = securableItemsModule.Post(requestUrl, with => with.JsonBody(securableItemToPost)).Result;
            Assert.Equal(HttpStatusCode.Conflict, result.StatusCode);
        }

        [Fact]
        public void AddSecurableItemByItemId_Successful()
        {
            //Arrange
            var existingClient = _existingClients.First(c => c.Id == FabricSampleAppClientId);
            var innerSecurable2 = existingClient.TopLevelSecurableItem.SecurableItems.First(s => s.Name == "inner-securable-2");
            var securableItemsModule = CreateBrowser(new Claim(Claims.ClientId, existingClient.Id),
                new Claim(Claims.Scope, Scopes.WriteScope), new Claim(Claims.Scope, Scopes.ReadScope));
            var securableItemToPost = new SecurableItemApiModel
            {
                ClientOwner = FabricSampleAppClientId,
                Name = "inner-securable-3"
            };

            //Act
            var result = securableItemsModule.Post($"/securableitems/{innerSecurable2.Id}", with => with.JsonBody(securableItemToPost))
                .Result;

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

        [Fact]
        public async Task AddSecurableItem_ContainsRequiredScope_SuccessAsync()
        {
            var browser = CreateBrowser(
                new Claim(Claims.ClientId, DosClientId),
                new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.Scope, Scopes.ManageDosScope));

            var securableItemApiModel = new SecurableItemApiModel
            {
                Name = $"dos-child-sec-item-{Guid.NewGuid()}",
                ClientOwner = DosClientId,
                Grain = Domain.Defaults.Authorization.DosGrain
            };

            var dosSecItem = await _mockSecurableItemStore.Object.Get("datamarts");
            var result = await browser.Post($"/securableitems/{dosSecItem.Id}", with => with.JsonBody(securableItemApiModel));
            Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        }

        [Fact]
        public async Task AddSecurableItem_MissingRequiredScope_ForbiddenAsync()
        {
            var browser = CreateBrowser(
                new Claim(Claims.ClientId, DosClientId),
                new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.Scope, Scopes.ReadScope));

            var securableItemApiModel = new SecurableItemApiModel
            {
                Name = $"dos-child-sec-item-{Guid.NewGuid()}",
                ClientOwner = DosClientId,
                Grain = Domain.Defaults.Authorization.DosGrain
            };

            var dosSecItem = await _mockSecurableItemStore.Object.Get("datamarts");
            var result = await browser.Post($"/securableitems/{dosSecItem.Id}", with => with.JsonBody(securableItemApiModel));
            Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
        }

        [Fact]
        public async Task AddSecurableItem_MismatchGrain_BadRequestAsync()
        {
            var existingClient = _existingClients.First(c => c.Id == FabricSampleAppClientId);
            var innerSecurable2 = existingClient.TopLevelSecurableItem.SecurableItems.First(s => s.Name == "inner-securable-2");
            var securableItemsModule = CreateBrowser(
                new Claim(Claims.ClientId, existingClient.Id),
                new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.Scope, Scopes.ReadScope));

            var securableItemApiModel = new SecurableItemApiModel
            {
                Name = $"dos-child-sec-item-{Guid.NewGuid()}",
                ClientOwner = FabricSampleAppClientId,
                Grain = Domain.Defaults.Authorization.DosGrain
            };

            var result = await securableItemsModule.Post(
                $"/securableitems/{innerSecurable2.Id}",
                with => with.JsonBody(securableItemApiModel));

            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        public static IEnumerable<object[]> ConflictData => new[]
        {
            new object[] { new SecurableItemApiModel { ClientOwner = FabricSampleAppClientId, Name = "inner-securable-1" }, true}
        };
        
        public static IEnumerable<object[]> BadRequestData => new[]
        {
            new object[] { new SecurableItemApiModel{ Name = null}, 1, true},
            new object[] { new SecurableItemApiModel{ Name = string.Empty}, 1, true},            
            new object[] { new SecurableItemApiModel{ Name = null}, 1, true},
            new object[] { new SecurableItemApiModel{ Name = string.Empty}, 1, true}            
        };

        public static IEnumerable<object[]> BadScopes => new[]
        {
            new object[] { new Claim(Claims.Scope, Scopes.WriteScope), new Claim(Claims.ClientId, "invalid"), true},
            new object[] { new Claim(Claims.Scope, Scopes.ReadScope), new Claim(Claims.ClientId, "valid"), true},
            new object[] { new Claim(Claims.Scope, Scopes.WriteScope), null, true},
            new object[] { null, new Claim(Claims.ClientId, "valid"), true},
            new object[] { new Claim(Claims.Scope, Scopes.WriteScope), new Claim(Claims.ClientId, "invalid"), true},
            new object[] { new Claim(Claims.Scope, Scopes.ReadScope), new Claim(Claims.ClientId, "valid"), true},
            new object[] { new Claim(Claims.Scope, Scopes.WriteScope), null, true},
            new object[] { null, new Claim(Claims.ClientId, "valid"), true},
        };

        protected override ConfigurableBootstrapper.ConfigurableBootstrapperConfigurator ConfigureBootstrapper(ConfigurableBootstrapper configurableBootstrapper,
            params Claim[] claims)
        {
            return base.ConfigureBootstrapper(configurableBootstrapper, claims)
                .Dependency<ClientService>(typeof(ClientService))
                .Dependency<GrainService>(typeof(GrainService))
                .Dependency<SecurableItemService>(typeof(SecurableItemService))
                .Dependency<IPermissionResolverService>(typeof(PermissionResolverService))
                .Dependency(_mockClientStore.Object)
                .Dependency(_mockLogger.Object)
                .Dependency(MockGrainStore.Object)
                .Dependency(MockRoleStore.Object)
                .Dependency(MockPermissionStore.Object)
                .Dependency(MockUserStore.Object)
                .Dependency(_mockSecurableItemStore.Object)
                .Dependency(MockEdwStore.Object);
        }
    }
}
