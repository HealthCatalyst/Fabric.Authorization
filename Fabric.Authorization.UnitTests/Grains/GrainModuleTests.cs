using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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
using Xunit.Abstractions;

namespace Fabric.Authorization.UnitTests.Grains
{
    public class GrainModuleTests : ModuleTestsBase<GrainsModule>
    {
        private readonly List<Client> _existingClients;
        private readonly Mock<IClientStore> _mockClientStore;
        private readonly Mock<IGrainStore> _mockGrainStore;
        private readonly Mock<ILogger> _mockLogger;
        private readonly ITestOutputHelper output;

        private const string FabricSampleAppClientId = "sample-fabric-app";
        private const string DosClientId = "dos-metadata-service";

        public GrainModuleTests(ITestOutputHelper output)
        {
            this.output = output;
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
                },
                new SecurableItem
                {
                    Name = "level_three_a",
                    Grain = Domain.Defaults.Authorization.DosGrain,
                    IsDeleted=true,
                    SecurableItems = new List<SecurableItem>
                        {
                            new SecurableItem
                            {
                                Name = "level_two",
                                IsDeleted=true,
                                SecurableItems = new List<SecurableItem>
                                {
                                    new SecurableItem
                                    {
                                        Name = "level_three_a",
                                        IsDeleted=true
                                    }
                                }
                            }
                        }
                }
            };

            _mockClientStore = new Mock<IClientStore>()
                .SetupGetClient(_existingClients)
                .SetupAddClient();

            MockGrainStore.SetupGetAllGrain(GetGrainWithDeepGraph());

            var secItems = _existingClients
                .Select(c => c.TopLevelSecurableItem)
                .Union(_existingClients.SelectMany(c => c.TopLevelSecurableItem.SecurableItems))
                .Union(dosSecurableItems);

            MockSecurableItemStore.SetupGetSecurableItem(secItems.ToList());

            _mockLogger = new Mock<ILogger>();
        }

        [Fact]
        public async void GetAllGrains_ReturnsNonDeletedItems()
        {
            // Arrange
            var existingClient = _existingClients.First(c => c.Id == FabricSampleAppClientId);
            var subject = CreateBrowser(new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.ClientId, existingClient.Id));

            // Act
            var actualResult = subject.Get("/grains").Result;

            // Assert
            Assert.Equal(HttpStatusCode.OK, actualResult.StatusCode);
            var grainsResult = actualResult.Body.DeserializeJson<IEnumerable<GrainApiModel>>();

            var first = grainsResult.First(g => g.Name == Domain.Defaults.Authorization.AppGrain);
            Assert.True(first.SecurableItems.Count == 1);

            var second = grainsResult.First(g => g.Name == Domain.Defaults.Authorization.DosGrain);
            Assert.True(second.SecurableItems.Count == 2);
        }

        [Fact]
        public void GetGrainByClientId_ReturnsForbidden()
        {
            // Arrange
            var existingClient = _existingClients.First(c => c.Id == FabricSampleAppClientId);
            var subject = CreateBrowser(new Claim(Claims.ClientId, existingClient.Id));

            // Act
            var result = subject.Get("/grains").Result;

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
        }

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
                .Dependency(MockSecurableItemStore.Object);
        }

        private static IEnumerable<Grain> GetGrainWithDeepGraph()
        {
            return new List<Grain>
            {
                new Grain
                {
                    Id = Guid.NewGuid(),
                    Name = Domain.Defaults.Authorization.AppGrain,
                    SecurableItems = new List<SecurableItem>
                    {
                        new SecurableItem
                        {
                            Name = "level_two"
                        }
                    }
                },
                new Grain
                {
                    Id = Guid.NewGuid(),
                    Name = Domain.Defaults.Authorization.DosGrain,
                    RequiredWriteScopes = new List<string> {"fabric/authorization.dos.write"},
                    IsShared = true,
                    SecurableItems = new List<SecurableItem>()
                    {
                        new SecurableItem
                        {
                            Name = "level_one_a",
                        },
                        new SecurableItem
                        {
                            Name = "level_one_b"
                        }
                    }
                }
            };
        }
    }
}
