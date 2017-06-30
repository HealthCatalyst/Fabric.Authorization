using System;
using System.Collections.Generic;
using System.Security.Claims;
using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Modules;
using Fabric.Authorization.API.Services;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores;
using Moq;
using Nancy;
using Nancy.Testing;
using Serilog;
using Xunit;

namespace Fabric.Authorization.IntegrationTests
{
    public class CouchDBTests : IntegrationTestsFixture
    {
        public CouchDBTests()
        {
            ICouchDbSettings config = new CouchDbSettings()
            {
                DatabaseName = Guid.NewGuid().ToString(),
                Username = "",
                Password = "",
                Server = "http://127.0.0.1:5984"
            };

            IDocumentDbService dbService = new CouchDbAccessService(config, new Mock<ILogger>().Object);
            var store = new CouchDBGroupStore(dbService, new Mock<ILogger>().Object);
            var groupService = new GroupService(store, new CouchDBRoleStore(dbService, new Mock<ILogger>().Object));

            this.Browser = new Browser(with =>
            {
                with.Module(new GroupsModule(groupService, new Domain.Validators.GroupValidator(store), new Mock<ILogger>().Object));
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
        }

        [Fact]
        void Test()
        {

        }
    }
}