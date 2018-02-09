using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Models;
using Nancy;
using Nancy.Testing;
using Newtonsoft.Json;
using Xunit;

namespace Fabric.Authorization.IntegrationTests.Modules
{
    [Collection("InMemoryTests")]
    public class ClientTests : IClassFixture<IntegrationTestsFixture>
    {
        private readonly Browser _browser;
        private readonly IntegrationTestsFixture _fixture;
        private readonly string _storageProvider;
        public ClientTests(IntegrationTestsFixture fixture, string storageProvider = StorageProviders.InMemory)
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(Claims.Scope, Scopes.ManageClientsScope),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.Scope, Scopes.WriteScope),
            }, "testprincipal"));

            _storageProvider = storageProvider;
            _fixture = fixture;
            _browser = fixture.GetBrowser(principal, storageProvider);
        }
        
        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodName]
        [InlineData("CD47FCAB-0C85-45C0-95F8-9F4A4E5B8E57")]
        [InlineData("C470B25B-4309-4C02-91DD-EB7C2D99E341")]
        public async Task TestGetClient_FailAsync(string id)
        {
            var get = await _browser.Get($"/clients/{id}", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        }

        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodName]
        [InlineData("8993363C-2E3C-4168-BAD7-F0F3DDFDB6F3", "97B3E59A-C76C-4383-A765-609686F15FAB")]
        public async Task TestGetClients_SuccessAsync(string clientId1, string clientId2)
        {
            var getInitialCountResponse = await _browser.Get("/clients", with =>
            {
                with.HttpRequest();
            });
            Assert.Equal(HttpStatusCode.OK, getInitialCountResponse.StatusCode);
            var initialClients = getInitialCountResponse.Body.DeserializeJson<IEnumerable<ClientApiModel>>();
            var initialClientCount = initialClients.Count();
            var clientIds = new[] { clientId1, clientId2 };
            //add two clients
            foreach (var clientId in clientIds)
            {
                var client = new ClientApiModel { Id = clientId, Name = clientId };

                var postResponse = await _browser.Post("/clients", with =>
                {
                    with.HttpRequest();
                    with.JsonBody(client);
                });
                Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            }
            //confirm you can get two clients back 
            var getResponse = await _browser.Get("/clients", with =>
            {
                with.HttpRequest();
            });
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            var clients = getResponse.Body.DeserializeJson<IEnumerable<ClientApiModel>>();
            Assert.Equal(initialClientCount + 2, clients.Count());
            //delete one client
            var delete = await _browser.Delete($"/clients/{clientIds[0]}", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
            //confirm you get one client back 
            getResponse = await _browser.Get("/clients", with =>
            {
                with.HttpRequest();
            });
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            clients = getResponse.Body.DeserializeJson<IEnumerable<ClientApiModel>>();
            Assert.Equal(initialClientCount + 1, clients.Count());
        }

        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodName]
        [InlineData("118475CC-A1B4-449C-8E13-EA4D06A159B3")]
        [InlineData("F5DB70E2-AF0C-4C72-8447-1B418C47EE1A")]
        [InlineData("6BC32347-36A1-44CF-AA0E-6C1038AA1DF3")]
        public async Task TestAddNewClient_SuccessAsync(string id)
        {
            var clientToAdd = new ClientApiModel
            {
                Id = id,
                Name = id,
                TopLevelSecurableItem = new SecurableItemApiModel
                {
                    Name = id
                }
            };

            var postResponse = await _browser.Post("/clients", with =>
            {
                with.HttpRequest();
                with.JsonBody(clientToAdd);
            });

            
            var getResponse = await _browser.Get($"/clients/{id}", with =>
                {
                    with.HttpRequest();                    
                });
            

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.Contains(id, getResponse.Body.AsString());

            var clientBrowser = _fixture.GetBrowser(GetPrincipalForClient(clientToAdd.Id), _storageProvider);

            var rolesResponse = await clientBrowser.Get($"/roles/{Domain.Defaults.Authorization.AppGrain}/{clientToAdd.Id}",
                with =>
                {
                    with.HttpRequest();
                });
            Assert.Equal(HttpStatusCode.OK, rolesResponse.StatusCode);

            var roles = JsonConvert.DeserializeObject<List<RoleApiModel>>(rolesResponse.Body.AsString());
            Assert.Single(roles);

            var permissions = roles.First().Permissions;
            Assert.Single(roles.First().Permissions);
            Assert.Equal(Domain.Defaults.Authorization.AuthorizationPermissionName, permissions.First().Name);
        }

        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodName]
        [InlineData("3F205ECA-BA68-42F3-9CBC-B6D2C178D782")]
        [InlineData("D622053D-03F5-489E-84F7-5471DA309213")]
        public async Task TestAddNewClient_FailAsync(string id)
        {
            var clientToAdd = new ClientApiModel
            {
                Id = id,
                Name = id,
                TopLevelSecurableItem = new SecurableItemApiModel
                {
                    Name = id
                }
            };

            var postResponse = await _browser.Post("/clients", with =>
            {
                with.HttpRequest();
                with.JsonBody(clientToAdd);
            });

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            // Repeat
            postResponse = await _browser.Post("/clients", with =>
            {
                with.HttpRequest();
                with.JsonBody(clientToAdd);
            });

            Assert.Equal(HttpStatusCode.Conflict, postResponse.StatusCode);
            Assert.Contains(
                $"Client {id} already exists. Please provide a new client id",
                postResponse.Body.AsString());
        }

        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodName]
        [InlineData("CD13516E-2C63-4F7F-8B4C-CF0187BC79D6")]
        [InlineData("F8C01F6B-C09C-430C-97A5-A0F2F1B340FB")]
        public async Task TestDeleteClient_SuccessAsync(string id)
        {
            var clientToAdd = new ClientApiModel
            {
                Id = id,
                Name = id,
                TopLevelSecurableItem = new SecurableItemApiModel
                {
                    Name = id
                }
            };

            var getResponse = await _browser.Post("/clients", with =>
            {
                with.HttpRequest();
                with.JsonBody(clientToAdd);
            });

            var delete = await _browser.Delete($"/clients/{id}", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        }

        [Theory]
        [IntegrationTestsFixture.DisplayTestMethodName]
        [InlineData("EF30B395-B505-4DE2-829B-8BBAA919F15C")]
        [InlineData("BB93ADC5-CD86-4746-B39B-9C56564A1BD2")]
        public async Task TestDeleteClient_FailAsync(string id)
        {
            var delete = await _browser.Delete($"/clients/{id}", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        }

        private ClaimsPrincipal GetPrincipalForClient(string clientId)
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(Claims.Scope, Scopes.ManageClientsScope),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.ClientId, clientId)
            }, "testprincipal"));
            return principal;
        }

    }
}