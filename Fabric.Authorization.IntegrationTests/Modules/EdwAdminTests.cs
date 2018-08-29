using Catalyst.Fabric.Authorization.Models;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.Persistence.SqlServer.Configuration;
using Nancy;
using Nancy.Testing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using System.Linq;

namespace Fabric.Authorization.IntegrationTests.Modules
{
    public class EdwAdminTests : IClassFixture<IntegrationTestsFixture>
    {
        private readonly Browser _browser;
        private readonly IntegrationTestsFixture _fixture;
        private readonly string _storageProvider;
        private readonly string _clientId;

        public EdwAdminTests(IntegrationTestsFixture fixture, string storageProvider = StorageProviders.InMemory, ConnectionStrings connectionStrings = null)
        {
            if (connectionStrings != null)
            {
                fixture.ConnectionStrings = connectionStrings;
            }

            _clientId = "fabric-installer";

            var principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(Claims.Scope, Scopes.ManageClientsScope),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.Scope, Scopes.ManageDosScope),
                new Claim(Claims.ClientId, _clientId)
            }, "testprincipal"));

            _storageProvider = storageProvider;
            _fixture = fixture;
            _browser = fixture.GetBrowser(principal, storageProvider);
            fixture.CreateClient(_browser, _clientId);
        }

        [Fact]
        public async Task SyncPermissions_NotFoundAsync()
        {
            var group = new GroupUserApiModel { GroupName = "group" };

            var result = await _browser.Post("/edw/group/roles", with => with.JsonBody(group));

            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        }

        [Fact]
        public async Task SyncPermissionsOnDeletedGroup_SucceedsAsync()
        {
            // TODO: Find a way to verify the roles are updated correctly
            // TODO: SyncService is disabled
            // TODO: Get group even if group is deleted

            // create role
            var role = await CreateRoleAsync();

            // create group
            var group = await CreateGroupAsync();

            // add role to group
            await AssociateGroupToRoleAsync(group, role);

            // create user
            var user = await CreateUserAsync();

            // add user to group
            await AssociateUserToGroupAsync(user, group);

            // delete group
            var deleteGroupResponse = await _browser.Delete($"/groups/{group.GroupName}", with =>
            {
                with.HttpRequest();
            });
            Assert.Equal(HttpStatusCode.NoContent, deleteGroupResponse.StatusCode);

            // Test SyncService
            var result = await _browser.Post($"/edw/{group.GroupName}/roles", with => with.JsonBody(group));
            Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
        }

        private async Task<RoleApiModel> CreateRoleAsync()
        {
            // get role
            //var roleResponse = await _browser.Get("/roles/dos/datamarts/datamartadmin", with =>
            //{
            //    with.HttpRequest();
            //});
            //Assert.Equal(HttpStatusCode.OK, roleResponse.StatusCode);
            //var role = JsonConvert.DeserializeObject<List<RoleApiModel>>(roleResponse.Body.AsString()).First();
            //Assert.Equal("datamartadmin", role.Name);

            var roleResponse = await _browser.Post("/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    Grain = "dos",
                    SecurableItem = _clientId,
                    Name = "jobsadmin",
                    DisplayName = "dosadmindisplay",
                    Description = "dosadmindescription"
                });
            });
            Assert.Equal(HttpStatusCode.Created, roleResponse.StatusCode);
            var role = JsonConvert.DeserializeObject<RoleApiModel>(roleResponse.Body.AsString());
            return role;
        }

        private async Task<GroupRoleApiModel> CreateGroupAsync()
        {
            var groupResponse = await _browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.JsonBody(new
                {
                    GroupName = "dosadmins" + Guid.NewGuid(),
                    GroupSource = "Custom"
                });
            });
            Assert.Equal(HttpStatusCode.Created, groupResponse.StatusCode);
            var group = JsonConvert.DeserializeObject<GroupRoleApiModel>(groupResponse.Body.AsString());
            return group;
        }

        private async Task AssociateGroupToRoleAsync(GroupRoleApiModel group, RoleApiModel role)
        {
            var groupRoleResponse = await _browser.Post($"/groups/{group.GroupName}/roles", with =>
            {
                with.HttpRequest();
                with.JsonBody(new[]
                {
                    new
                    {
                        role.Grain,
                        role.SecurableItem,
                        role.Name,
                        role.Id
                    }
                });
            });
            Assert.Equal(HttpStatusCode.OK, groupRoleResponse.StatusCode);
        }

        private async Task<UserApiModel> CreateUserAsync()
        {
            var userResponse = await _browser.Post("/user", with =>
            {
                with.JsonBody(new
                {
                    identityProvider = "temp",
                    subjectId = "tempUser"
                });
            });
            Assert.Equal(HttpStatusCode.Created, userResponse.StatusCode);
            var user = JsonConvert.DeserializeObject<UserApiModel>(userResponse.Body.AsString());
            return user;
        }

        private async Task AssociateUserToGroupAsync(UserApiModel user, GroupRoleApiModel group)
        {
            var groupUserResponse = await _browser.Post($"/groups/{group.GroupName}/users", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.JsonBody(new[]
                {
                    new
                    {
                         user.SubjectId,
                         user.IdentityProvider
                    }
                });
            });
            Assert.Equal(HttpStatusCode.OK, groupUserResponse.StatusCode);
        }
    }
}
