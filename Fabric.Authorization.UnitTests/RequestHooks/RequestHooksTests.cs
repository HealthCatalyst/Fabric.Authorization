using System.Collections.Generic;
using System.Security.Claims;
using Catalyst.Fabric.Authorization.Models;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Modules;
using Fabric.Authorization.API.Services;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Resolvers.Permissions;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.UnitTests.Mocks;
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
            var mockClientStore = new Mock<IClientStore>();

            mockClientStore.SetupGetClient(new List<Client>());

            var logger = new Mock<ILogger>().Object;
            var mockUserStore = new Mock<IUserStore>().Object;

            var mockPermissionStore = new Mock<IPermissionStore>().Object;
            var mockRoleStore = new Mock<IRoleStore>().Object;
            var mockSecurableItemStore = new Mock<ISecurableItemStore>().Object;
            var userService = new UserService(mockUserStore, mockRoleStore);
            var clientService = new ClientService(mockClientStore.Object, mockSecurableItemStore);
            var roleService = new RoleService(mockRoleStore, mockPermissionStore);
            var permissionService = new PermissionService(mockPermissionStore, roleService);
            var permissionResolverService = new PermissionResolverService(new List<IPermissionResolverService>(), logger);
            var accessService = new AccessService(permissionResolverService, userService, logger);

            _browser = new Browser(with =>
            {
                with.Module(new ClientsModule(
                    clientService,
                    new Domain.Validators.ClientValidator(clientService),
                    logger,
                    accessService,
                    roleService,
                    permissionService));

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
