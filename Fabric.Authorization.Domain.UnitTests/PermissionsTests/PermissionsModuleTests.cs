using System;
using System.Collections.Generic;
using System.Linq;
using Fabric.Authorization.API;
using Fabric.Authorization.API.Configuration;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.API.Modules;
using Fabric.Authorization.Domain.Permissions;
using Fabric.Authorization.Domain.UnitTests.Mocks;
using Fabric.Platform.Shared.Configuration;
using Moq;
using Nancy;
using Nancy.Testing;
using Serilog;
using Xunit;

namespace Fabric.Authorization.Domain.UnitTests.PermissionsTests
{
    public class PermissionsModuleTests
    {
        private Browser _authorizationApi;
        private List<Permission> _existingPermissions;
        private Mock<IPermissionStore> _mockPermissionStore;
        public PermissionsModuleTests()
        {
            _existingPermissions = new List<Permission>
            {
                new Permission
                {
                    Id = Guid.NewGuid(),
                    Grain = "app",
                    Resource = "patientsafety",
                    Name = "manageusers"
                },
                new Permission
                {
                    Id = Guid.NewGuid(),
                    Grain = "app",
                    Resource = "patientsafety",
                    Name = "updatepatient"
                },
                new Permission
                {
                    Id = Guid.NewGuid(),
                    Grain = "app",
                    Resource = "sourcemartdesigner",
                    Name = "manageusers"
                },
                new Permission
                {
                    Id = Guid.NewGuid(),
                    Grain = "patient",
                    Resource = "Patient",
                    Name ="read"
                }
            };
            _mockPermissionStore = new Mock<IPermissionStore>().SetupGetPermissions(_existingPermissions).SetupAddPermissions();
            _authorizationApi = new Browser(with => with.Module<PermissionsModule>()
                                                        .Dependency<IPermissionService>(typeof(PermissionService))
                                                        .Dependency(_mockPermissionStore.Object), withDefaults => withDefaults.Accept("application/json"));
        }

        [Fact]
        public void PermissionsModule_DeletePermission_NotFound()
        {
            var actual = _authorizationApi.Delete($"/permissions/{Guid.NewGuid()}").Result;
            Assert.Equal(HttpStatusCode.NotFound, actual.StatusCode);

        }

        [Fact]
        public void PermissionModule_DeletePermission_Successful()
        {
            var existingPermission = _existingPermissions.First();
            var actual = _authorizationApi.Delete($"/permissions/{existingPermission.Id}").Result;
            Assert.Equal(HttpStatusCode.NoContent, actual.StatusCode);
            _mockPermissionStore.Verify();
        }

        [Fact]
        public void PermissionsModule_AddPermission_PermissionAlreadyExists()
        {
            var existingPermission = _existingPermissions.First();
            var actual = _authorizationApi.Post("/permissions",
                with => with.JsonBody(new Permission
                {
                    Grain = existingPermission.Grain,
                    Resource = existingPermission.Resource,
                    Name = existingPermission.Name
                })).Result;
            Assert.Equal(HttpStatusCode.BadRequest, actual.StatusCode);
        }

        [Fact]
        public void PermissionsModule_AddPermission_PermissionAddedSuccessfully()
        {
            var permissionToPost = new Permission
            {
                Grain = "app",
                Resource = "patientsafety",
                Name = "createalerts"
            };

            var actual = _authorizationApi.Post("/permissions",
                with => with.JsonBody(permissionToPost)).Result;

            var newPermission = actual.Body.DeserializeJson<PermissionApiModel>();

            Assert.Equal(HttpStatusCode.Created, actual.StatusCode);
            Assert.NotNull(newPermission);
            Assert.NotNull(newPermission.Id);
            Assert.Equal(permissionToPost.Name, newPermission.Name);
        }

        [Theory, MemberData(nameof(RequestData))]
        public void PermissionsModule_GetPermissions_ReturnsPermissions(string path, int statusCode, int count)
        {
            var actual = _authorizationApi.Get(path).Result;
            Assert.Equal(statusCode, (int)actual.StatusCode);
            if (actual.StatusCode == HttpStatusCode.OK)
            {
                var permissions = actual.Body.DeserializeJson<List<PermissionApiModel>>();
                Assert.Equal(count, permissions.Count);
            }
        }

        public static IEnumerable<object[]> RequestData => new[]
        {
            new object[] { "/permissions/app/patientsafety", 200, 2},
            new object[] {"/permissions/app/patientsafety/updatepatient", 200, 1},
            new object[] {"/permissions/app/sourcemartdesigner", 200, 1},
            new object[] {"/permissions/app", 405, 1},
            new object[] {"/permissions/app/nonexistant", 200, 0}
        };
    }
}
