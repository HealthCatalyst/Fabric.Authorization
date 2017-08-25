using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Fabric.Authorization.API;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.API.Modules;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Domain.Stores.CouchDB;
using Fabric.Authorization.Domain.Stores.Services;
using Nancy;
using Nancy.Testing;
using Xunit;

namespace Fabric.Authorization.IntegrationTests.Modules
{
    [Collection("InMemoryTests")]
    public class ClientTests : IntegrationTestsFixture
    {
        public ClientTests(bool useInMemoryDB = true)
        {
            var store = useInMemoryDB ? new InMemoryClientStore() : (IClientStore)new CouchDbClientStore(this.DbService(), this.Logger, this.EventContextResolverService);
            var clientService = new ClientService(store);

            this.Browser = new Browser(with =>
            {
                with.Module(new ClientsModule(
                        clientService,
                        new Domain.Validators.ClientValidator(clientService),
                        this.Logger));

                with.RequestStartup((_, pipelines, context) =>
                {
                    context.CurrentUser = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                    {
                        new Claim(Claims.Scope, Scopes.ManageClientsScope),
                        new Claim(Claims.Scope, Scopes.ReadScope),
                        new Claim(Claims.Scope, Scopes.WriteScope),
                    }, "testprincipal"));
                    pipelines.BeforeRequest += (ctx) => RequestHooks.SetDefaultVersionInUrl(ctx);
                });
            }, withDefaults => withDefaults.HostName("testhost"));
        }

        [Theory]
        [DisplayTestMethodName]
        [InlineData("InexistentClient")]
        [InlineData("InexistentClient2")]
        public void TestGetClient_Fail(string Id)
        {
            var get = this.Browser.Get($"/clients/{Id}", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        }

        [Theory]
        [DisplayTestMethodName]
        [InlineData("Client1", "Client2")]
        public void TestGetClients_Success(string clientId1, string clientId2)
        {
            var getInitialCountResponse = this.Browser.Get("/clients", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;
            Assert.Equal(HttpStatusCode.OK, getInitialCountResponse.StatusCode);
            var initialClients = getInitialCountResponse.Body.DeserializeJson<IEnumerable<ClientApiModel>>();
            var initialClientCount = initialClients.Count();

            var clientIds = new[] {clientId1, clientId2};
            //add two clients
            foreach (var clientId in clientIds)
            {
                var postResponse = this.Browser.Post("/clients", with =>
                {
                    with.HttpRequest();
                    with.Header("Accept", "application/json");
                    with.FormValue("Id", clientId);
                    with.FormValue("Name", clientId);
                }).Result;
                Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            }
            //confirm you can get two clients back 
            var getResponse = this.Browser.Get("/clients", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            var clients = getResponse.Body.DeserializeJson<IEnumerable<ClientApiModel>>();
            Assert.Equal(initialClientCount + 2, clients.Count());

            //delete one client
            var delete = this.Browser.Delete($"/clients/{clientIds[0]}", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

            //confirm you get one client back 
            getResponse = this.Browser.Get("/clients", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            clients = getResponse.Body.DeserializeJson<IEnumerable<ClientApiModel>>();
            Assert.Equal(initialClientCount + 1, clients.Count());
        }

        [Theory]
        [DisplayTestMethodName]
        [InlineData("Client1")]
        [InlineData("Client2")]
        [InlineData("6BC32347-36A1-44CF-AA0E-6C1038AA1DF3")]
        public void TestAddNewClient_Success(string Id)
        {
            var postResponse = this.Browser.Post("/clients", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Id", Id);
                with.FormValue("Name", Id);
            }).Result;

            var getResponse = this.Browser.Get($"/clients/{Id}", with =>
                {
                    with.HttpRequest();
                    with.Header("Accept", "application/json");
                }).Result;

            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.True(getResponse.Body.AsString().Contains(Id));
        }

        [Theory]
        [DisplayTestMethodName]
        [InlineData("RepeatedClient1")]
        [InlineData("RepeatedClient2")]
        public void TestAddNewClient_Fail(string Id)
        {
            this.Browser.Post("/clients", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Id", Id);
                with.FormValue("Name", Id);
            }).Wait();

            // Repeat
            var postResponse = this.Browser.Post("/clients", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Id", Id);
                with.FormValue("Name", Id);
            }).Result;

            Assert.Equal(HttpStatusCode.BadRequest, postResponse.StatusCode);
        }

        [Theory]
        [DisplayTestMethodName]
        [InlineData("ClientToBeDeleted")]
        [InlineData("ClientToBeDeleted2")]
        public void TestDeleteClient_Success(string Id)
        {
            this.Browser.Post("/clients", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Id", Id);
                with.FormValue("Name", Id);
            }).Wait();

            var delete = this.Browser.Delete($"/clients/{Id}", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        }

        [Theory]
        [DisplayTestMethodName]
        [InlineData("InexistentClient")]
        [InlineData("InexistentClient2")]
        public void TestDeleteClient_Fail(string Id)
        {
            var delete = this.Browser.Delete($"/clients/{Id}", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        }
    }
}