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
using Moq;
using Nancy;
using Nancy.Testing;
using Newtonsoft.Json;
using Serilog;
using Xunit;

namespace Fabric.Authorization.UnitTests.Permissions
{
    public class PermissionsModuleTests : ModuleTestsBase<PermissionsModule>
    {
        public PermissionsModuleTests()
        {
            _mockLogger = new Mock<ILogger>();
        }

        private readonly Mock<ILogger> _mockLogger;

        [Theory]
        [MemberData(nameof(BadRequestData))]
        public void AddPermission_InvalidModel(string grain, string securableItem, string permissionName,
            int errorCount)
        {
            var permissionsModule = CreateBrowser(new Claim(Claims.ClientId, "patientsafety"),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.Scope, Scopes.WriteScope));

            var permissionToPost = new Permission
            {
                Grain = grain,
                SecurableItem = securableItem,
                Name = permissionName
            };

            var actual = permissionsModule.Post("/permissions",
                with => with.JsonBody(permissionToPost)).Result;

            Assert.Equal(HttpStatusCode.BadRequest, actual.StatusCode);
            var errorResponse = JsonConvert.DeserializeObject<Error>(actual.Body.AsString());
            Assert.Equal(errorCount, errorResponse.Details.Length);
        }

        [Theory]
        [MemberData(nameof(RequestData))]
        public void GetPermissions_ReturnsPermissionsForGrainAndSecurableItem(string path, int statusCode, int count)
        {
            var permissionsModule = CreateBrowser(new Claim(Claims.ClientId, "patientsafety"),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.Scope, Scopes.WriteScope));

            var actual = permissionsModule.Get(path).Result;
            Assert.Equal(statusCode, (int) actual.StatusCode);
            if (actual.StatusCode == HttpStatusCode.OK)
            {
                var permissions = actual.Body.DeserializeJson<List<PermissionApiModel>>();
                Assert.Equal(count, permissions.Count);
            }
        }

        public static IEnumerable<object[]> RequestData => new[]
        {
            new object[] {"/permissions/app/patientsafety", 200, 2},
            new object[] {"/permissions/app/patientsafety/updatepatient", 200, 1},
            new object[] {"/permissions/app/sourcemartdesigner", 200, 1},
            new object[] {"/permissions/app", 400, 0}
        };

        public static IEnumerable<object[]> BadRequestData => new[]
        {
            new object[] {"app", "patientsafety", "", 1},
            new object[] {"app", "", "", 2},
            new object[] {"", "", "", 3},
            new object[] {"app", "patientsafety", null, 1},
            new object[] {"app", null, null, 2},
            new object[] {null, null, null, 3}
        };

        protected override ConfigurableBootstrapper.ConfigurableBootstrapperConfigurator ConfigureBootstrapper(
            ConfigurableBootstrapper configurableBootstrapper,
            params Claim[] claims)
        {
            return base.ConfigureBootstrapper(configurableBootstrapper, claims)
                .Dependency<RoleService>(typeof(RoleService))
                .Dependency<PermissionService>(typeof(PermissionService))
                .Dependency<ClientService>(typeof(ClientService))
                .Dependency<IPermissionResolverService>(typeof(PermissionResolverService))
                .Dependency(_mockLogger.Object)
                .Dependency(MockClientStore.Object)
                .Dependency(MockPermissionStore.Object)
                .Dependency(MockRoleStore.Object)
                .Dependency(MockGrainStore.Object)
                .Dependency(MockUserStore.Object)
                .Dependency(MockSecurableItemStore.Object);
        }

        [Fact]
        public void AddPermission_PermissionAddedSuccessfully()
        {
            var permissionsModule = CreateBrowser(new Claim(Claims.ClientId, "patientsafety"),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.Scope, Scopes.WriteScope));

            var permissionToPost = new Permission
            {
                Grain = "app",
                SecurableItem = "patientsafety",
                Name = "createalerts"
            };

            var actual = permissionsModule.Post("/permissions",
                with => with.JsonBody(permissionToPost)).Result;

            var newPermission = actual.Body.DeserializeJson<PermissionApiModel>();
            var locationHeaderValue = actual.Headers[HttpResponseHeaders.Location];

            Assert.Equal(HttpStatusCode.Created, actual.StatusCode);
            Assert.NotNull(newPermission);
            Assert.NotNull(newPermission.Id);
            Assert.Equal(permissionToPost.Name, newPermission.Name);
            Assert.Equal($"http://testhost:80/v1/permissions/{newPermission.Id}", locationHeaderValue);
        }

        [Fact]
        public void AddPermission_PermissionAlreadyExists()
        {
            var permissionsModule = CreateBrowser(new Claim(Claims.ClientId, "patientsafety"),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.Scope, Scopes.WriteScope));
            var existingPermission =
                ExistingPermissions.First(p => p.Grain == "app" && p.SecurableItem == "patientsafety");
            var actual = permissionsModule.Post("/permissions",
                with => with.JsonBody(new Permission
                {
                    Grain = existingPermission.Grain,
                    SecurableItem = existingPermission.SecurableItem,
                    Name = existingPermission.Name
                })).Result;
            Assert.Equal(HttpStatusCode.Conflict, actual.StatusCode);
        }

        [Fact]
        public void DeletePermission_NotFound()
        {
            var permissionsModule = CreateBrowser(new Claim(Claims.ClientId, "patientsafety"),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.Scope, Scopes.WriteScope));
            var actual = permissionsModule.Delete($"/permissions/{Guid.NewGuid()}").Result;
            Assert.Equal(HttpStatusCode.NotFound, actual.StatusCode);
        }

        [Fact]
        public void DeletePermission_ReturnsBadRequestForInvalidId()
        {
            var permissionsModule = CreateBrowser(new Claim(Claims.ClientId, "patientsafety"),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.Scope, Scopes.WriteScope));
            var actual = permissionsModule.Delete("/permissions/notaguid").Result;
            Assert.Equal(HttpStatusCode.BadRequest, actual.StatusCode);
        }

        [Fact]
        public void DeletePermission_Successful()
        {
            var permissionsModule = CreateBrowser(new Claim(Claims.ClientId, "patientsafety"),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.Scope, Scopes.WriteScope));
            var existingPermission = ExistingPermissions.First();
            var actual = permissionsModule.Delete($"/permissions/{existingPermission.Id}").Result;
            Assert.Equal(HttpStatusCode.NoContent, actual.StatusCode);
            MockPermissionStore.Verify(permissionStore => permissionStore.Delete(existingPermission));
        }

        [Fact]
        public void GetPermissions_ReturnsBadRequestForInvalidId()
        {
            var permissionsModule = CreateBrowser(new Claim(Claims.ClientId, "patientsafety"),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.Scope, Scopes.WriteScope));

            var actual = permissionsModule.Get("/permissions/notaguid").Result;
            Assert.Equal(HttpStatusCode.BadRequest, actual.StatusCode);
        }

        [Fact]
        public void GetPermissions_ReturnsNotFoundForMissingId()
        {
            var permissionsModule = CreateBrowser(new Claim(Claims.ClientId, "patientsafety"),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.Scope, Scopes.WriteScope));

            var actual = permissionsModule.Get($"/permissions/{Guid.NewGuid()}").Result;
            Assert.Equal(HttpStatusCode.NotFound, actual.StatusCode);
        }

        [Fact]
        public void GetPermissions_ReturnsPermissionForId()
        {
            var permissionsModule = CreateBrowser(new Claim(Claims.ClientId, "patientsafety"),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.Scope, Scopes.WriteScope));

            var existingPermission = ExistingPermissions.First();
            var actual = permissionsModule.Get($"/permissions/{existingPermission.Id}").Result;
            Assert.Equal(HttpStatusCode.OK, actual.StatusCode);
            var newPermission = actual.Body.DeserializeJson<PermissionApiModel>();
            Assert.Equal(existingPermission.Id, newPermission.Id);
        }
    }
}