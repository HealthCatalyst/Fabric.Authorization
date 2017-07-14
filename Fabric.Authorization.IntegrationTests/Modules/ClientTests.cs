using System;
using System.Collections.Generic;
using System.Security.Claims;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Modules;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores;
using Nancy;
using Nancy.Testing;
using Xunit;

namespace Fabric.Authorization.IntegrationTests
{
    public class ClientTests : IntegrationTestsFixture
    {
        public ClientTests(bool useInMemoryDB = true)
        {
            Console.WriteLine($"Starting Client Tests. Memory: {useInMemoryDB}");
            var store = useInMemoryDB ? new InMemoryClientStore() : (IClientStore)new CouchDBClientStore(this.DbService(), this.Logger); ;
            var clientService = new ClientService(store);

            this.Browser = new Browser(with =>
            {
                with.Module(new ClientsModule(
                        clientService,
                        new Domain.Validators.ClientValidator(store),
                        this.Logger));

                with.RequestStartup((_, __, context) =>
                {
                    context.CurrentUser = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>()
                    {
                        new Claim(Claims.Scope, Scopes.ManageClientsScope),
                        new Claim(Claims.Scope, Scopes.ReadScope),
                        new Claim(Claims.Scope, Scopes.WriteScope),
                    }, "testprincipal"));
                });
            });

            Console.WriteLine("Finished setup");
        }

        [Theory]
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