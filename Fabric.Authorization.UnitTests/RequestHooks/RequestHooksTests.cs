﻿using System.Collections.Generic;
using System.Security.Claims;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.API.Modules;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Persistence.InMemory.Stores;
using Moq;
using Nancy;
using Nancy.Testing;
using Newtonsoft.Json;
using Serilog;
using Xunit;

namespace Fabric.Authorization.UnitTests.RequestHooks
{
    public class RequestHooksTests
    {
        private readonly Browser _browser;
        public RequestHooksTests()
        {
            var store = new InMemoryClientStore();
            var logger = new Mock<ILogger>().Object;
            var clientService = new ClientService(store);

            _browser = new Browser(with =>
            {
                with.Module(new ClientsModule(
                    clientService,
                    new Domain.Validators.ClientValidator(clientService),
                    logger));

                with.RequestStartup((_, pipelines, context) =>
                {
                    context.CurrentUser = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                    {
                        new Claim(Claims.Scope, Scopes.ManageClientsScope),
                        new Claim(Claims.Scope, Scopes.ReadScope),
                        new Claim(Claims.Scope, Scopes.WriteScope),
                    }, "testprincipal"));
                    pipelines.BeforeRequest += ctx => API.Infrastructure.PipelineHooks.RequestHooks.ErrorResponseIfContentTypeMissingForPostAndPut(ctx);
                    pipelines.BeforeRequest += ctx => API.Infrastructure.PipelineHooks.RequestHooks.RemoveContentTypeHeaderForGet(ctx);
                    pipelines.BeforeRequest += ctx => API.Infrastructure.PipelineHooks.RequestHooks.SetDefaultVersionInUrl(ctx);
                });

            }, withDefaults => withDefaults.HostName("testhost"));
        }

        [Fact]
        public void TestGetClient_ContentTypeHeaderSet_Success()
        {
            var get = _browser.Get($"/clients/Client1", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.Header("Content-Type", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        }

        [Fact]
        public void TestAddClient_InvalidContentTypeHeaderSet_BadRequestException()
        {
            var clientToAdd = new ClientApiModel { Id = "foo", Name = "foo" };

            var postResponse = _browser.Post("/clients", with =>
            {
                with.HttpRequest();
                with.Body(JsonConvert.SerializeObject(clientToAdd), "text/plain"); //default if nothing provided

            }).Result;

            Assert.Equal(HttpStatusCode.BadRequest, postResponse.StatusCode);
            Assert.Contains(
                "Content-Type header must be application/json or application/xml when attempting a POST or PUT",
                postResponse.Body.AsString());
        }
    }
}
