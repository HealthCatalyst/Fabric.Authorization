using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.API.Modules;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Resolvers.Permissions;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.UnitTests.Mocks;
using Moq;
using Nancy;
using Nancy.Testing;
using Serilog;
using Xunit;

namespace Fabric.Authorization.UnitTests.Clients
{
    public class ClientsModuleTests : ModuleTestsBase<ClientsModule>
    {
        private readonly List<Client> _existingClients;
        private readonly Mock<IClientStore> _mockClientStore;
        private readonly Mock<ILogger> _mockLogger;
        private readonly Mock<IGrainStore> _mockGrainStore;
        private readonly Mock<IRoleStore> _mockRolesStore;
        private readonly Mock<IPermissionStore> _mockPermissionStore;
        private readonly Mock<IUserStore> _mockUserStore;

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
                },
                new Client
                {
                    Id = "deleted-client",
                    Name = "Deleted Client Application",
                    TopLevelSecurableItem = new SecurableItem
                    {
                        Id = Guid.NewGuid(),
                        Name = "deleted-client"
                    },
                    IsDeleted = true
                }
            };

            _mockClientStore = new Mock<IClientStore>()
                .SetupGetClient(_existingClients)
                .SetupAddClient();

            _mockGrainStore = new Mock<IGrainStore>();

            _mockRolesStore = new Mock<IRoleStore>();

            _mockPermissionStore = new Mock<IPermissionStore>();

            _mockUserStore = new Mock<IUserStore>();

            _mockLogger = new Mock<ILogger>();
        }
        
        [Fact]
        public void GetClients_ReturnsClients()
        {
            var existingClient = _existingClients.First();
            var clientsModule = CreateBrowser(new Claim(Claims.Scope, Scopes.ManageClientsScope),
                new Claim(Claims.Scope, Scopes.ReadScope));
            var result = clientsModule.Get("/clients").Result;
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var clients = result.Body.DeserializeJson<List<Client>>();
            Assert.Equal(1, clients.Count);
            Assert.Equal(existingClient.Id, clients.First().Id);
        }

        [Fact]
        public void GetClients_ReturnsForbidden()
        {
            var clientsModule = CreateBrowser();
            var result = clientsModule.Get("/clients").Result;
            Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
        }
        
        [Fact]
        public void GetClient_ReturnsClient()
        {
            var existingClient = _existingClients.First();
            var clientsModule = CreateBrowser(new Claim(Claims.ClientId, existingClient.Id),
                new Claim(Claims.Scope, Scopes.ReadScope));
            var result = clientsModule.Get($"/clients/{existingClient.Id}").Result;
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var client = result.Body.DeserializeJson<Client>();
            Assert.Equal(existingClient.Id, client.Id);
        }

        [Fact]
        public void GetClient_ReturnsError()
        {
            var nonExistentClientId = "nonexistent";
            var clientsModule = CreateBrowser(new Claim(Claims.ClientId, nonExistentClientId),
                new Claim(Claims.Scope, Scopes.ReadScope));
            var result = clientsModule.Get($"/clients/{nonExistentClientId}").Result;
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
            var error = result.Body.DeserializeJson<Error>();
            Assert.Equal(Enum.GetName(typeof(HttpStatusCode), HttpStatusCode.NotFound), error.Code);
            Assert.Equal(typeof(Client).Name, error.Target);
        }

        [Fact]
        public void GetClient_ReturnsForbidden()
        {
            var existingClient = _existingClients.First();
            var clientsModule = CreateBrowser();
            var result = clientsModule.Get($"/clients/{existingClient.Id}").Result;
            Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
        }

        [Fact]
        public void AddClient_Successful()
        {
            var clientsModule = CreateBrowser(new Claim(Claims.Scope, Scopes.ManageClientsScope),
                new Claim(Claims.Scope, Scopes.WriteScope));
            var clientToPost = new ClientApiModel
            {
                Id = "sample-fabric-app2",
                Name = "Sample Fabric App V2"
            };
            var result = clientsModule.Post("/clients", with => with.JsonBody(clientToPost)).Result;
            Assert.Equal(HttpStatusCode.Created, result.StatusCode);
            var newClient = result.Body.DeserializeJson<ClientApiModel>();
            Assert.NotNull(newClient.TopLevelSecurableItem);
            Assert.Equal(clientToPost.Id, newClient.TopLevelSecurableItem.Name);
            Assert.NotEqual(DateTime.MinValue, newClient.CreatedDateTimeUtc);
        }

        [Theory, MemberData(nameof(BadRequestData))]
        public void AddClient_InvalidDataReturnsError(Client client, int errorCount)
        {
            var clientsModule = CreateBrowser(new Claim(Claims.Scope, Scopes.ManageClientsScope),
                new Claim(Claims.Scope, Scopes.WriteScope));
            var result = clientsModule.Post("/clients", with => with.JsonBody(client)).Result;
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            var error = result.Body.DeserializeJson<Error>();
            Assert.Equal(errorCount, error.Details.Length);
        }

        [Fact]
        public void AddClient_DuplicateReturnsConflict()
        {
            var clientsModule = CreateBrowser(new Claim(Claims.Scope, Scopes.ManageClientsScope),
                new Claim(Claims.Scope, Scopes.WriteScope));
            var clientToPost =
                new ClientApiModel { Id = "sample-fabric-app", Name = "Sample Fabric Client Application" };
            var result = clientsModule.Post("/clients", with => with.JsonBody(clientToPost)).Result;
            Assert.Equal(HttpStatusCode.Conflict, result.StatusCode);
        }

        [Fact]
        public void AddClient_PreventsOverposting()
        {
            var clientsModule = CreateBrowser(new Claim(Claims.Scope, Scopes.ManageClientsScope),
                new Claim(Claims.Scope, Scopes.WriteScope));
            var clientToPost = new ClientApiModel
            {
                Id = "another-sample-app",
                Name = "Another Sample App",
                CreatedBy = "someone",
                CreatedDateTimeUtc = DateTime.UtcNow.AddDays(-45),
                ModifiedBy = "someone",
                ModifiedDateTimeUtc = DateTime.UtcNow.AddDays(-45),
                TopLevelSecurableItem = new SecurableItemApiModel
                {
                    Id = Guid.NewGuid(),
                    Name = "somesecurable"
                }
            };
            var result = clientsModule.Post("/clients", with => with.JsonBody(clientToPost)).Result;
            Assert.Equal(HttpStatusCode.Created, result.StatusCode);
            var newClient = result.Body.DeserializeJson<ClientApiModel>();
            Assert.NotNull(newClient.TopLevelSecurableItem);
            Assert.Equal(clientToPost.Id, newClient.TopLevelSecurableItem.Name);
            Assert.NotEqual(clientToPost.CreatedDateTimeUtc, newClient.CreatedDateTimeUtc);
            Assert.NotEqual(clientToPost.ModifiedDateTimeUtc, newClient.ModifiedDateTimeUtc);
            Assert.NotEqual(clientToPost.TopLevelSecurableItem.Id, newClient.TopLevelSecurableItem.Id);
            Assert.True(string.IsNullOrEmpty(newClient.CreatedBy));
            Assert.True(string.IsNullOrEmpty(newClient.ModifiedBy));
        }


        [Fact]
        public void DeleteClient_Successful()
        {
            var clientsModule = CreateBrowser(new Claim(Claims.Scope, Scopes.ManageClientsScope),
                new Claim(Claims.Scope, Scopes.WriteScope));
            var existingClient = _existingClients.First();
            var result = clientsModule.Delete($"/clients/{existingClient.Id}").Result;
            Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
            _mockClientStore.Verify(clientStore => clientStore.Delete(existingClient));
        }

        [Fact]
        public void DeleteClient_ReturnsForbidden()
        {
            var existingClient = _existingClients.First();
            var clientsModule = CreateBrowser(new Claim(Claims.ClientId, existingClient.Id),
                new Claim(Claims.Scope, Scopes.WriteScope));
            var result = clientsModule.Delete($"/clients/{existingClient.Id}").Result;
            Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
            _mockClientStore.Verify(clientStore => clientStore.Delete(existingClient), Times.Never);
        }

        [Fact]
        public void DeleteClient_ReturnNotFound()
        {
            var clientsModule = CreateBrowser(new Claim(Claims.Scope, Scopes.ManageClientsScope),
                new Claim(Claims.Scope, Scopes.WriteScope));
            var nonexistentId = "nonexistentid";
            var result = clientsModule.Delete($"/clients/{nonexistentId}").Result;
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
            var error = result.Body.DeserializeJson<Error>();
            Assert.NotNull(error);
            Assert.Contains(nonexistentId, error.Message);
        }

      
        protected override ConfigurableBootstrapper.ConfigurableBootstrapperConfigurator ConfigureBootstrapper(ConfigurableBootstrapper configurableBootstrapper, params Claim[] claims)
        {
            return base.ConfigureBootstrapper(configurableBootstrapper, claims)
                .Dependency<ClientService>(typeof(ClientService))
                .Dependency<IPermissionResolverService>(typeof(PermissionResolverService))
                .Dependency(_mockLogger.Object)
                .Dependency(_mockClientStore.Object)
                .Dependency(MockGrainStore.Object)
                .Dependency(MockRoleStore.Object)
                .Dependency(MockPermissionStore.Object)
                .Dependency(MockUserStore.Object);
        }

        public static IEnumerable<object[]> BadRequestData => new[]
        {
            new object[] { new Client{ Id = null, Name = null}, 2},
            new object[] { new Client{ Id = string.Empty, Name = string.Empty}, 2},
            new object[] { new Client{ Id = "newapp", Name = string.Empty}, 1},            
        };
    }
}
