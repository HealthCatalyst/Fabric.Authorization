using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Models;
using Fabric.Authorization.API.Modules;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores;
using IdentityModel;
using Nancy;
using Nancy.Testing;
using Xunit;

namespace Fabric.Authorization.IntegrationTests
{
    public class UserTests : IntegrationTestsFixture
    {
        public UserTests(bool useInMemoryDB = true)
        {
            var roleStore = useInMemoryDB ? new InMemoryRoleStore() : (IRoleStore)new CouchDBRoleStore(this.DbService(), this.Logger);
            var groupStore = useInMemoryDB ? new InMemoryGroupStore() : (IGroupStore)new CouchDBGroupStore(this.DbService(), this.Logger);
            var clientStore = useInMemoryDB ? new InMemoryClientStore() : (IClientStore)new CouchDBClientStore(this.DbService(), this.Logger);
            var permissionStore = useInMemoryDB ? new InMemoryPermissionStore() : (IPermissionStore)new CouchDBPermissionStore(this.DbService(), this.Logger);

            var roleService = new RoleService(roleStore, new InMemoryPermissionStore());
            var groupService = new GroupService(groupStore, roleStore);
            var clientService = new ClientService(clientStore);
            var permissionService = new PermissionService(permissionStore);

            this.Browser = new Browser(with =>
            {
                with.Module(new RolesModule(
                        roleService,
                        clientService,
                        new Domain.Validators.RoleValidator(roleStore),
                        this.Logger));

                with.Module(new ClientsModule(
                        clientService,
                        new Domain.Validators.ClientValidator(clientStore),
                        this.Logger));

                with.Module(new UsersModule(
                        clientService,
                        groupService,
                        new Domain.Validators.UserValidator(),
                        this.Logger));

                with.Module(new GroupsModule(
                        groupService,
                        new Domain.Validators.GroupValidator(groupStore),
                        this.Logger));

                with.Module(new PermissionsModule(
                        permissionService,
                        clientService,
                        new Domain.Validators.PermissionValidator(permissionStore),
                        this.Logger));

                with.RequestStartup((_, __, context) =>
                {
                    context.CurrentUser = new ClaimsPrincipal(
                        new ClaimsIdentity(new List<Claim>()
                        {
                        new Claim(Claims.Scope, Scopes.ManageClientsScope),
                        new Claim(Claims.Scope, Scopes.ReadScope),
                        new Claim(Claims.Scope, Scopes.WriteScope),
                        new Claim(Claims.ClientId, "userprincipal"),
                        new Claim(JwtClaimTypes.Role, Guid.Parse("A9CA0300-1006-40B1-ABF1-E0C3B396F95F").ToString())
                        }, "userprincipal"));
                });
            });


            this.Browser.Post("/clients", with =>
            {
                with.HttpRequest();
                with.FormValue("Id", "userprincipal");
                with.FormValue("Name", "userprincipal");
                with.Header("Accept", "application/json");
            }).Wait();

        }

        [Fact]
        public void TestInheritance_Success()
        {
            var group = Guid.Parse("A9CA0300-1006-40B1-ABF1-E0C3B396F95F").ToString();

            // Adding permissions

            var post = this.Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "userprincipal");
                with.FormValue("Name", "greatgrandfatherpermissions");
            }).Result;

            var ggfperm = post.Body.DeserializeJson<PermissionApiModel>();

            post = this.Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "userprincipal");
                with.FormValue("Name", "grandfatherpermissions");
            }).Result;

            var gfperm = post.Body.DeserializeJson<PermissionApiModel>();

            post = this.Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "userprincipal");
                with.FormValue("Name", "fatherpermissions");
            }).Result;

            var fperm = post.Body.DeserializeJson<PermissionApiModel>();

            post = this.Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "userprincipal");
                with.FormValue("Name", "himselfpermissions");
            }).Result;

            var hsperm = post.Body.DeserializeJson<PermissionApiModel>();

            post = this.Browser.Post("/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Grain", "app");
                with.FormValue("SecurableItem", "userprincipal");
                with.FormValue("Name", "sonpermissions");
            }).Result;

            var sonperm = post.Body.DeserializeJson<PermissionApiModel>();

            // Adding Roles

            var role = new RoleApiModel()
            {
                Grain = "app",
                SecurableItem = "userprincipal",
                Name = "greatgrandfather",
                Permissions = new List<PermissionApiModel>() { ggfperm }
            };

            post = this.Browser.Post("/roles", with => // -3
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.JsonBody(role);
            }).Result;

            var ggf = post.Body.DeserializeJson<RoleApiModel>();

            post = this.Browser.Post("/roles", with => // -2
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                role.Name = "grandfather";
                role.ParentRole = ggf.Id;
                role.Permissions = new List<PermissionApiModel>() { gfperm };
                with.JsonBody(role);
            }).Result;

            var gf = post.Body.DeserializeJson<RoleApiModel>();

            post = this.Browser.Post("/roles", with => // -1
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                role.Name = "father";
                role.ParentRole = gf.Id;
                role.Permissions = new List<PermissionApiModel>() { fperm };
                with.JsonBody(role);
            }).Result;

            var f = post.Body.DeserializeJson<RoleApiModel>();

            post = this.Browser.Post("/roles", with => // 0
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                role.Name = "himself";
                role.ParentRole = f.Id;
                role.Permissions = new List<PermissionApiModel>() { hsperm };
                with.JsonBody(role);
            }).Result;

            var hs = post.Body.DeserializeJson<RoleApiModel>();

            post = this.Browser.Post("/roles", with => // 1
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                role.Name = "son";
                role.ParentRole = hs.Id;
                role.Permissions = new List<PermissionApiModel>() { sonperm };
                with.JsonBody(role);
            }).Result;

            var son = post.Body.DeserializeJson<RoleApiModel>();

            // Adding groups

            this.Browser.Post("/groups", with =>
            {
                with.HttpRequest();
                with.FormValue("Id", group);
                with.FormValue("GroupName", group);
            }).Wait();

            var p = this.Browser.Post($"/groups/{group}/roles", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.FormValue("Id", hs.Identifier);
            }).Result;

            Task.Delay(200).Wait();

            // Get the permissions

            var get = this.Browser.Get($"/user/permissions", with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
            }).Result;

            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            Assert.True(get.Body.AsString().Contains("greatgrandfatherpermissions"));
            Assert.True(get.Body.AsString().Contains("grandfatherpermissions"));
            Assert.True(get.Body.AsString().Contains("fatherpermissions"));
            Assert.True(get.Body.AsString().Contains("himselfpermissions"));
            Assert.False(get.Body.AsString().Contains("sonpermissions"));
        }
    }
}