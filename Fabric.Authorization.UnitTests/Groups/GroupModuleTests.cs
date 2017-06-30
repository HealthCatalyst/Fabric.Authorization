using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.API.Modules;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Nancy;
using Nancy.Testing;
using Xunit;

namespace Fabric.Authorization.UnitTests.Groups
{
    public class GroupsModuleTests : ModuleTestsBase<GroupsModule>
    {
        [Fact]
        public void GetGroups_ReturnsGroupForGroupName()
        {
            var existingClient = ExistingClients.First();
            var existingGroup = ExistingGroups.First();
            Assert.NotNull(existingGroup);
            var GroupsModule = CreateBrowser(new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.ClientId, existingClient.Id));
            var result = GroupsModule.Get($"/Groups/app/groups/{existingGroup.Name}").Result;
            AssertGroupsOK(result, 1, existingGroup.Id);
        }
        
        [Fact]
        public void AddGroup_Succeeds()
        {
            var existingClient = ExistingClients.First();
            var GroupsModule = CreateBrowser(new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.ClientId, existingClient.Id));
            var GroupToPost = new GroupRoleApiModel
            {
                GroupName = "Group1"
            };
            var result = GroupsModule.Post($"/Groups", with => with.JsonBody(GroupToPost)).Result;
            Assert.Equal(HttpStatusCode.Created, result.StatusCode);
            var newGroup = result.Body.DeserializeJson<GroupRoleApiModel>();
            Assert.Equal(GroupToPost.GroupName, newGroup.GroupName);
            Assert.NotNull(newGroup.GroupName);
        }

        [Theory, MemberData(nameof(AddGroupBadRequestData))]
        public void AddGroup_ReturnsBadRequest(GroupRoleApiModel GroupToPost, int errorCount)
        {
            var existingClient = ExistingClients.First();
            var GroupsModule = CreateBrowser(new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.ClientId, existingClient.Id));
            var result = GroupsModule.Post($"/Groups", with => with.JsonBody(GroupToPost)).Result;
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            var error = result.Body.DeserializeJson<Error>();
            Assert.NotNull(error);
            if (errorCount > 0)
            {
                Assert.Equal(errorCount, error.Details.Length);
            }
        }

        [Theory, MemberData(nameof(AddGroupForbiddenData))]
        public void AddGroup_ReturnsForbidden(GroupRoleApiModel GroupToPost, string scope)
        {
            var existingClient = ExistingClients.First();
            var GroupsModule = CreateBrowser(new Claim(Claims.Scope, scope),
                new Claim(Claims.ClientId, existingClient.Id));
            var result = GroupsModule.Post($"/Groups", with => with.JsonBody(GroupToPost)).Result;
            Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
        }

        [Fact]
        public void DeleteGroup_Succeeds()
        {
            var existingClient = ExistingClients.First();
            var existingGroup = ExistingGroups.First();
            AssertDeleteGroup(HttpStatusCode.NoContent, existingClient.Id, existingGroup.Id.ToString(), Scopes.WriteScope);
        }

        [Fact]
        public void DeleteGroup_ReturnsNotFound()
        {
            var existingClient = ExistingClients.First();
            AssertDeleteGroup(HttpStatusCode.NotFound, existingClient.Id, Guid.NewGuid().ToString(), Scopes.WriteScope);
        }

        [Fact]
        public void DeleteGroup_ReturnsBadRequest()
        {
            var existingClient = ExistingClients.First();
            AssertDeleteGroup(HttpStatusCode.BadRequest, existingClient.Id, "notaguid", Scopes.WriteScope);
        }

        [Fact]
        public void DeleteGroup_WrongScope_ReturnsForbidden()
        {
            var existingClient = ExistingClients.First();
            var existingGroup = ExistingGroups.First();
            AssertDeleteGroup(HttpStatusCode.Forbidden, existingClient.Id, existingGroup.Id.ToString(), Scopes.ReadScope);
        }

        [Theory, MemberData(nameof(DeleteGroupForbiddenData))]
        public void DeleteGroup_WrongClient_ReturnsForbidden(string cliendId)
        {
            var existingClient = ExistingClients.First();
            var existingGroup = ExistingGroups.First();
            var GroupsModule = CreateBrowser(new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.ClientId, cliendId));
            var result = GroupsModule.Delete($"/Groups/{existingGroup.Id}").Result;
            Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
        }

        [Fact]
        public void AddPermissionsToGroup_Succeeds()
        {
            var existingClient = ExistingClients.First();
            var existingGroup = ExistingGroups.First();
            //var existingPermission =
            //    ExistingPermissions.First(p => p.Grain == existingGroup.Grain &&
            //                                    p.SecurableItem == existingGroup.SecurableItem);
            //var GroupsModule = CreateBrowser(new Claim(Claims.Scope, Scopes.WriteScope),
            //    new Claim(Claims.ClientId, existingClient.Id));
            //var result = GroupsModule.Post($"/Groups/{existingGroup.Id}/permissions",
            //        with => with.JsonBody(new List<Permission>{existingPermission}))
            //    .Result;
            //AssertGroupOK(result, 1);
        }

        [Fact]
        public void AddPermissionsToGroup_BadRequest()
        {
        }

        [Fact]
        public void AddPermissionsToGroup_GroupNotFound()
        {
        }

        [Fact]
        public void AddPermissionsToGroup_PermissionNotFound()
        {
            var existingClient = ExistingClients.First();
            var existingGroup = ExistingGroups.First();
            var permission = new Permission
            {
                Id = Guid.NewGuid(),
                Grain = "app",
                SecurableItem = "patientsafety",
                Name = "notfound"
            };
            PostPermissionAndAssert(existingGroup, permission, existingClient.Id, HttpStatusCode.BadRequest);
        }

        [Fact]
        public void DeletePermissionFromGroup_Succeeds()
        {
        }

        [Fact]
        public void DeletePermissionFromGroup_IncorrectFormat_BadRequest()
        {
        }

        [Fact]
        public void DeletePermissionFromGroup_GroupNotFound()
        {
        }

        [Fact]
        public void DeletePermissionFromGroup_PermissionNotFound()
        {
        }


        private void DeletePermissionAndAssert(string clientId, string GroupId, Permission permission, HttpStatusCode expectedStatusCode, string scope = null)
        {
            var requestScope = string.IsNullOrEmpty(scope) ? Scopes.WriteScope : scope;
            var GroupsModule = CreateBrowser(new Claim(Claims.Scope, requestScope),
                new Claim(Claims.ClientId, clientId));
            var result = GroupsModule.Delete($"/Groups/{GroupId}/permissions",
                    with => with.JsonBody(new List<Permission> { permission }))
                .Result;
            Assert.Equal(expectedStatusCode, result.StatusCode);
        }
        
        private void PostPermissionAndAssert(Group Group, Permission permission, string clientId, HttpStatusCode expectedStatusCode, string scope = null)
        {
            var requestScope = string.IsNullOrEmpty(scope) ? Scopes.WriteScope : scope;
            var GroupsModule = CreateBrowser(new Claim(Claims.Scope, requestScope),
                new Claim(Claims.ClientId, clientId));
            var result = GroupsModule.Post($"/Groups/{Group.Id}/permissions",
                    with => with.JsonBody(new List<Permission> { permission }))
                .Result;
            Assert.Equal(expectedStatusCode, result.StatusCode);
        }

        private void AssertGroupOK(BrowserResponse result, int expectedPermissionCount)
        {
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var updatedGroup = result.Body.DeserializeJson<GroupRoleApiModel>();
            Assert.NotNull(updatedGroup);
//            Assert.Equal(expectedPermissionCount, updatedGroup.Permissions.Count());
        }

        private void AssertGroupsOK(BrowserResponse result, int expectedGroupsCount, string expectedId)
        {
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var Groups = result.Body.DeserializeJson<List<GroupRoleApiModel>>();
            Assert.Equal(expectedGroupsCount, Groups.Count);
            Assert.Equal(expectedId, Groups.First().GroupName);
        }
        
        private void AssertDeleteGroup(HttpStatusCode expectedStatusCode, string clientId, string GroupId, string scope)
        {
            var GroupsModule = CreateBrowser(new Claim(Claims.Scope, scope),
                new Claim(Claims.ClientId, clientId));
            var result = GroupsModule.Delete($"/Groups/{GroupId}").Result;
            Assert.Equal(expectedStatusCode, result.StatusCode);
        }

        public static IEnumerable<object[]> AddGroupBadRequestData => new[]
        {
  //          new object[] {new GroupRoleApiModel {Grain = "app", Name = "test"}, 1},
  //          new object[] {new GroupRoleApiModel {Grain = "app", SecurableItem = "patientsafety"}, 1},
  //          new object[] {new GroupRoleApiModel {Grain = "app"}, 2},
            new object[] {new GroupRoleApiModel(), 3}
        };

        public static IEnumerable<object[]> AddGroupForbiddenData => new[]
        {
            new object[] {new GroupRoleApiModel {GroupName = "app"}, Scopes.ReadScope},
            new object[] {new GroupRoleApiModel { GroupName = "app"}, Scopes.WriteScope},
        };

        public static IEnumerable<object[]> DeleteGroupForbiddenData => new[]
        {
            new object[] {"sourcemartdesigner"},
            new object[] {"notaclient"},
        };

        public static IEnumerable<object[]> AddPermissionToGroupForbiddenData => new[]
        {
            new object[] { "patientsafety", "patientsafety", Scopes.ReadScope}, 
            new object[] { "patientsafety", "sourcemartdesigner", Scopes.WriteScope}, 
        };

        public static IEnumerable<object[]> GetGroupsForbiddenData => new[]
        {
            new object[] {"badscope", "patientsafety"},
            new object[] {Scopes.ReadScope, "sourcemartdesigner"},
        };

        protected override ConfigurableBootstrapper.ConfigurableBootstrapperConfigurator ConfigureBootstrapper(ConfigurableBootstrapper configurableBootstrapper,
            params Claim[] claims)
        {
            return base.ConfigureBootstrapper(configurableBootstrapper, claims)
                .Dependency<IGroupService>(typeof(GroupService))
                .Dependency<IClientService>(typeof(ClientService))
                .Dependency(MockLogger.Object)
                .Dependency(MockClientStore.Object)
                .Dependency(MockPermissionStore.Object)
                .Dependency(MockGroupStore.Object);
        }
    }
}
