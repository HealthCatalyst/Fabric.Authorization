using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Catalyst.Fabric.Authorization.Models;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.Domain;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Persistence.SqlServer.Configuration;
using Fabric.Authorization.Persistence.SqlServer.EntityModels;
using Fabric.Authorization.Persistence.SqlServer.Mappers;
using Fabric.Authorization.Persistence.SqlServer.Services;
using Nancy.Helpers;
using Nancy.Testing;
using Xunit;

namespace Fabric.Authorization.IntegrationTests.Services
{
    [Collection("InMemoryTests")]
    public class GroupMigratorServiceTests : IClassFixture<IntegrationTestsFixture>
    {
        protected readonly Browser Browser;
        private readonly IntegrationTestsFixture _fixture;
        private readonly string _storageProvider;

        protected ClaimsPrincipal Principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
        {
            new Claim(Claims.Scope, Scopes.ManageClientsScope),
            new Claim(Claims.Scope, Scopes.ReadScope),
            new Claim(Claims.Scope, Scopes.WriteScope),
            new Claim(Claims.ClientId, "rolesprincipal"),
            new Claim(Claims.IdentityProvider, "idP1")
        }, "rolesprincipal"));

        public GroupMigratorServiceTests(
            IntegrationTestsFixture fixture,
            string storageProvider = StorageProviders.InMemory,
            ConnectionStrings connectionStrings = null)
        {
            if (connectionStrings != null)
            {
                fixture.ConnectionStrings = connectionStrings;
            }

            Browser = fixture.GetBrowser(Principal, storageProvider);
            fixture.CreateClient(Browser, "rolesprincipal");
            _storageProvider = storageProvider;
            _fixture = fixture;
        }

        [Fact(Skip = "Skip due to case sensitive query in EF Core 2.2 in memory provider")]
        public async Task MigrateDuplicateGroups_NoDuplicates_Success()
        {
            var container = _fixture.Bootstrapper.TinyIoCContainer;
            var dbContext = container.Resolve<IAuthorizationDbContext>();
            var groupMigratorService = container.Resolve<GroupMigratorService>();

            var customGroup1 = new Group
            {
                GroupId = Guid.NewGuid(),
                Name = $"Custom Group 1-{Guid.NewGuid()}",
                Source = GroupConstants.CustomSource,
                CreatedBy = "test",
                CreatedDateTimeUtc = DateTime.UtcNow
            };

            var customGroup2 = new Group
            {
                GroupId = Guid.NewGuid(),
                Name = $"Custom Group 2-{Guid.NewGuid()}",
                Source = GroupConstants.CustomSource,
                CreatedBy = "test",
                CreatedDateTimeUtc = DateTime.UtcNow
            };

            var group1 = new Group
            {
                GroupId = Guid.NewGuid(),
                Name = $"Group 1-{Guid.NewGuid()}",
                Source = GroupConstants.DirectorySource,
                IdentityProvider = IdentityConstants.ActiveDirectory,
                CreatedBy = "test",
                CreatedDateTimeUtc = DateTime.UtcNow
            };

            var group2 = new Group
            {
                GroupId = Guid.NewGuid(),
                Name = $"Group 2-{Guid.NewGuid()}",
                Source = GroupConstants.DirectorySource,
                IdentityProvider = IdentityConstants.ActiveDirectory,
                CreatedBy = "test",
                CreatedDateTimeUtc = DateTime.UtcNow
            };

            var childGroup1 = new ChildGroup
            {
                ChildGroupId = group1.GroupId,
                ParentGroupId = customGroup1.GroupId,
                CreatedBy = "test",
                CreatedDateTimeUtc = DateTime.UtcNow
            };

            var childGroup2 = new ChildGroup
            {
                ChildGroupId = group2.GroupId,
                ParentGroupId = customGroup1.GroupId,
                CreatedBy = "test",
                CreatedDateTimeUtc = DateTime.UtcNow
            };

            var childGroup3 = new ChildGroup
            {
                ChildGroupId = group2.GroupId,
                ParentGroupId = customGroup2.GroupId,
                CreatedBy = "test",
                CreatedDateTimeUtc = DateTime.UtcNow
            };

            dbContext.Groups.AddRange(new List<Group>
            {
                customGroup1,
                customGroup2,
                group1,
                group2
            });

            dbContext.ChildGroups.AddRange(new List<ChildGroup>
            {
                childGroup1,
                childGroup2,
                childGroup3
            });

            dbContext.SaveChanges();

            var result = await groupMigratorService.MigrateDuplicateGroups();
            Assert.Empty(result.GroupMigrationRecords);

            var groupStore = container.Resolve<IGroupStore>();
            await groupStore.Delete(customGroup1.ToModel());
            await groupStore.Delete(customGroup2.ToModel());
            await groupStore.Delete(group1.ToModel());
            await groupStore.Delete(group2.ToModel());
        }

        [Fact(Skip = "Skip due to case sensitive query in EF Core 2.2 in memory provider")]
        public async Task MigrateDuplicateDirectoryGroups_DuplicateNames_SuccessAsync()
        {
            var container = _fixture.Bootstrapper.TinyIoCContainer;
            var dbContext = container.Resolve<IAuthorizationDbContext>();
            var groupMigratorService = container.Resolve<GroupMigratorService>();

            #region Data Setup

            var client = new Client
            {
                ClientId = $"client1-{Guid.NewGuid()}",
                Name = $"Client 1-{Guid.NewGuid()}"
            };

            var grain = new Grain
            {
                Name = $"dos-{Guid.NewGuid()}"
            };

            var securableItem = new SecurableItem
            {
                Name = $"datamarts-{Guid.NewGuid()}",
                Grain = grain,
                ClientOwner = client.ClientId
            };

            client.TopLevelSecurableItem = securableItem;

            var customGroup1 = new Group
            {
                GroupId = Guid.NewGuid(),
                Name = $"Custom Group 1-{Guid.NewGuid()}",
                Source = GroupConstants.CustomSource,
                CreatedBy = "test",
                CreatedDateTimeUtc = DateTime.UtcNow
            };

            var customGroup2 = new Group
            {
                GroupId = Guid.NewGuid(),
                Name = $"Custom Group 2-{Guid.NewGuid()}",
                Source = GroupConstants.CustomSource,
                CreatedBy = "test",
                CreatedDateTimeUtc = DateTime.UtcNow
            };

            var groupGuid = Guid.NewGuid();
            var group1 = new Group
            {
                GroupId = Guid.NewGuid(),
                Name = $"Group 1-{groupGuid}",
                Source = GroupConstants.DirectorySource,
                IdentityProvider = IdentityConstants.ActiveDirectory,
                CreatedBy = "test",
                CreatedDateTimeUtc = DateTime.UtcNow
            };

            var group2 = new Group
            {
                GroupId = Guid.NewGuid(),
                Name = $"groUP 1-{groupGuid}",
                Source = GroupConstants.DirectorySource,
                IdentityProvider = IdentityConstants.ActiveDirectory,
                CreatedBy = "test",
                CreatedDateTimeUtc = DateTime.UtcNow
            };

            var role1 = new Role
            {
                RoleId = Guid.NewGuid(),
                Name = "Role 1",
                Grain = grain.Name,
                SecurableItem = securableItem
            };

            var role2 = new Role
            {
                RoleId = Guid.NewGuid(),
                Name = "Role 2",
                Grain = grain.Name,
                SecurableItem = securableItem,
            };

            var role3 = new Role
            {
                RoleId = Guid.NewGuid(),
                Name = "Role 3",
                Grain = grain.Name,
                SecurableItem = securableItem,
            };

            var group1Role1 = new GroupRole
            {
                Group = group1,
                Role = role1
            };

            var group2Role1 = new GroupRole
            {
                Group = group2,
                Role = role1
            };

            var group2Role2 = new GroupRole
            {
                Group = group2,
                Role = role2
            };

            var group2Role3 = new GroupRole
            {
                Group = group2,
                Role = role3,
                IsDeleted = true
            };

            var user1 = new User
            {
                IdentityProvider = IdentityConstants.ActiveDirectory,
                SubjectId = Guid.NewGuid().ToString()
            };

            var customGroup1User1 = new GroupUser
            {
                Group = customGroup1,
                User = user1
            };

            var customGroup2User1 = new GroupUser
            {
                Group = customGroup2,
                User = user1
            };

            var childGroup1 = new ChildGroup
            {
                ChildGroupId = group1.GroupId,
                ParentGroupId = customGroup1.GroupId,
                CreatedBy = "test",
                CreatedDateTimeUtc = DateTime.UtcNow
            };

            var childGroup2 = new ChildGroup
            {
                ChildGroupId = group2.GroupId,
                ParentGroupId = customGroup1.GroupId,
                CreatedBy = "test",
                CreatedDateTimeUtc = DateTime.UtcNow
            };

            var childGroup3 = new ChildGroup
            {
                ChildGroupId = group2.GroupId,
                ParentGroupId = customGroup2.GroupId,
                CreatedBy = "test",
                CreatedDateTimeUtc = DateTime.UtcNow
            };

            dbContext.Clients.Add(client);
            dbContext.Grains.Add(grain);
            dbContext.SecurableItems.Add(securableItem);

            dbContext.Roles.AddRange(new List<Role>
            {
                role1,
                role2,
                role3
            });

            dbContext.Groups.AddRange(new List<Group>
            {
                customGroup1,
                customGroup2,
                group1,
                group2
            });

            dbContext.GroupRoles.AddRange(new List<GroupRole>
            {
                group1Role1,
                group2Role1,
                group2Role2,
                group2Role3
            });

            dbContext.Users.Add(user1);

            dbContext.GroupUsers.AddRange(new List<GroupUser>
            {
                customGroup1User1,
                customGroup2User1
            });

            dbContext.ChildGroups.AddRange(new List<ChildGroup>
            {
                childGroup1,
                childGroup2,
                childGroup3
            });

            dbContext.SaveChanges();

            #endregion

            var results = await groupMigratorService.MigrateDuplicateGroups();
            Assert.Equal(1, results.GroupMigrationRecords.Count);
            Assert.Empty(results.GroupMigrationRecords.SelectMany(r => r.Errors));

            var principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(Claims.Scope, Scopes.ManageClientsScope),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.ClientId, client.ClientId),
                new Claim(Claims.IdentityProvider, "idP1")
            }, "rolesprincipal"));

            var browser = _fixture.GetBrowser(principal, _storageProvider);

            var getResponse = await browser.Get(HttpUtility.UrlEncode($"/groups/Group 1-{groupGuid}"), with =>
            {
                with.HttpRequest();
            });

            var groupRoleApiModel = getResponse.Body.DeserializeJson<GroupRoleApiModel>();
            var roles = groupRoleApiModel.Roles.ToList();
            Assert.Equal(2, roles.Count);
            Assert.Contains(roles, r => r.Name == role1.Name);
            Assert.Contains(roles, r => r.Name == role2.Name);

            var parents = groupRoleApiModel.Parents.ToList();
            Assert.Contains(parents, p => p.GroupName == customGroup1.Name);
            Assert.Contains(parents, p => p.GroupName == customGroup2.Name);

            var groupStore = container.Resolve<IGroupStore>();
            await groupStore.Delete(customGroup1.ToModel());
            await groupStore.Delete(customGroup2.ToModel());
            await groupStore.Delete(group1.ToModel());
            await groupStore.Delete(group2.ToModel());
        }

        [Fact(Skip = "Skip due to case sensitive query in EF Core 2.2 in memory provider")]
        public async Task MigrateDuplicateCustomGroups_DuplicateNames_SuccessAsync()
        {
            
        }

        [Fact(Skip = "Skip due to case sensitive query in EF Core 2.2 in memory provider")]
        public async Task MigrateDuplicateGroups_HasDuplicateIdentifiers_SuccessAsync()
        {
            var container = _fixture.Bootstrapper.TinyIoCContainer;
            var dbContext = container.Resolve<IAuthorizationDbContext>();
            var groupMigratorService = container.Resolve<GroupMigratorService>();

            #region Data Setup

            var client = new Client
            {
                ClientId = $"client1-{Guid.NewGuid()}",
                Name = $"Client 1-{Guid.NewGuid()}"
            };

            var grain = new Grain
            {
                Name = $"dos-{Guid.NewGuid()}"
            };

            var securableItem = new SecurableItem
            {
                Name = $"datamarts-{Guid.NewGuid()}",
                Grain = grain,
                ClientOwner = client.ClientId
            };

            client.TopLevelSecurableItem = securableItem;

            var customGroup1 = new Group
            {
                GroupId = Guid.NewGuid(),
                Name = $"Custom Group 1-{Guid.NewGuid()}",
                Source = GroupConstants.CustomSource,
                CreatedBy = "test",
                CreatedDateTimeUtc = DateTime.UtcNow
            };

            var customGroup2 = new Group
            {
                GroupId = Guid.NewGuid(),
                Name = $"Custom Group 2-{Guid.NewGuid()}",
                Source = GroupConstants.CustomSource,
                CreatedBy = "test",
                CreatedDateTimeUtc = DateTime.UtcNow
            };

            var groupGuid = Guid.NewGuid();
            var group1 = new Group
            {
                GroupId = Guid.NewGuid(),
                Name = $"Group 1-{groupGuid}",
                IdentityProvider = IdentityConstants.ActiveDirectory,
                TenantId = "12345",
                Source = GroupConstants.DirectorySource,
                CreatedBy = "test",
                CreatedDateTimeUtc = DateTime.UtcNow
            };

            var group2 = new Group
            {
                GroupId = Guid.NewGuid(),
                Name = $"groUP 1-{groupGuid}",
                IdentityProvider = IdentityConstants.ActiveDirectory,
                TenantId = "12345",
                Source = GroupConstants.DirectorySource,
                CreatedBy = "test",
                CreatedDateTimeUtc = DateTime.UtcNow
            };

            var group3 = new Group
            {
                GroupId = Guid.NewGuid(),
                Name = $"groUP 1-{groupGuid}",
                IdentityProvider = IdentityConstants.AzureActiveDirectory,
                TenantId = "12345",
                Source = GroupConstants.DirectorySource,
                CreatedBy = "test",
                CreatedDateTimeUtc = DateTime.UtcNow
            };

            var role1 = new Role
            {
                RoleId = Guid.NewGuid(),
                Name = "Role 1",
                Grain = grain.Name,
                SecurableItem = securableItem
            };

            var role2 = new Role
            {
                RoleId = Guid.NewGuid(),
                Name = "Role 2",
                Grain = grain.Name,
                SecurableItem = securableItem,
            };

            var role3 = new Role
            {
                RoleId = Guid.NewGuid(),
                Name = "Role 3",
                Grain = grain.Name,
                SecurableItem = securableItem,
            };

            var role4 = new Role
            {
                RoleId = Guid.NewGuid(),
                Name = "Role 4",
                Grain = grain.Name,
                SecurableItem = securableItem,
            };

            var group1Role1 = new GroupRole
            {
                Group = group1,
                Role = role1
            };

            var group2Role1 = new GroupRole
            {
                Group = group2,
                Role = role1
            };

            var group2Role2 = new GroupRole
            {
                Group = group2,
                Role = role2
            };

            var group2Role3 = new GroupRole
            {
                Group = group2,
                Role = role3,
                IsDeleted = true
            };

            var group3Role4 = new GroupRole
            {
                Group = group3,
                Role = role4
            };

            var user1 = new User
            {
                IdentityProvider = IdentityConstants.ActiveDirectory,
                SubjectId = Guid.NewGuid().ToString()
            };

            var customGroup1User1 = new GroupUser
            {
                Group = customGroup1,
                User = user1
            };

            var customGroup2User1 = new GroupUser
            {
                Group = customGroup2,
                User = user1
            };

            var childGroup1 = new ChildGroup
            {
                ChildGroupId = group1.GroupId,
                ParentGroupId = customGroup1.GroupId,
                CreatedBy = "test",
                CreatedDateTimeUtc = DateTime.UtcNow
            };

            var childGroup2 = new ChildGroup
            {
                ChildGroupId = group2.GroupId,
                ParentGroupId = customGroup1.GroupId,
                CreatedBy = "test",
                CreatedDateTimeUtc = DateTime.UtcNow
            };

            var childGroup3 = new ChildGroup
            {
                ChildGroupId = group2.GroupId,
                ParentGroupId = customGroup2.GroupId,
                CreatedBy = "test",
                CreatedDateTimeUtc = DateTime.UtcNow
            };

            dbContext.Clients.Add(client);
            dbContext.Grains.Add(grain);
            dbContext.SecurableItems.Add(securableItem);

            dbContext.Roles.AddRange(new List<Role>
            {
                role1,
                role2,
                role3,
                role4
            });

            dbContext.Groups.AddRange(new List<Group>
            {
                customGroup1,
                customGroup2,
                group1,
                group2,
                group3
            });

            dbContext.GroupRoles.AddRange(new List<GroupRole>
            {
                group1Role1,
                group2Role1,
                group2Role2,
                group2Role3,
                group3Role4
            });

            dbContext.Users.Add(user1);

            dbContext.GroupUsers.AddRange(new List<GroupUser>
            {
                customGroup1User1,
                customGroup2User1
            });

            dbContext.ChildGroups.AddRange(new List<ChildGroup>
            {
                childGroup1,
                childGroup2,
                childGroup3
            });

            dbContext.SaveChanges();

            #endregion

            var results = await groupMigratorService.MigrateDuplicateGroups();
            Assert.Equal(1, results.GroupMigrationRecords.Count);
            Assert.Empty(results.GroupMigrationRecords.SelectMany(r => r.Errors));

            var principal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(Claims.Scope, Scopes.ManageClientsScope),
                new Claim(Claims.Scope, Scopes.ReadScope),
                new Claim(Claims.Scope, Scopes.WriteScope),
                new Claim(Claims.ClientId, client.ClientId),
                new Claim(Claims.IdentityProvider, "idP1")
            }, "rolesprincipal"));

            var browser = _fixture.GetBrowser(principal, _storageProvider);

            var getResponse = await browser.Get(HttpUtility.UrlEncode($"/groups/Group 1-{groupGuid}"), with =>
            {
                with.HttpRequest();
                with.Header("Accept", "application/json");
                with.Query("identityProvider", IdentityConstants.ActiveDirectory);
                with.Query("tenantId", "12345");
            });

            var groupRoleApiModel = getResponse.Body.DeserializeJson<GroupRoleApiModel>();
            var roles = groupRoleApiModel.Roles.ToList();
            Assert.Equal(2, roles.Count);
            Assert.Contains(roles, r => r.Name == role1.Name);
            Assert.Contains(roles, r => r.Name == role2.Name);

            var parents = groupRoleApiModel.Parents.ToList();
            Assert.Contains(parents, p => p.GroupName == customGroup1.Name);
            Assert.Contains(parents, p => p.GroupName == customGroup2.Name);

            var groupStore = container.Resolve<IGroupStore>();
            await groupStore.Delete(customGroup1.ToModel());
            await groupStore.Delete(customGroup2.ToModel());
            await groupStore.Delete(group1.ToModel());
            await groupStore.Delete(group2.ToModel());
            await groupStore.Delete(group3.ToModel());
        }

        [Fact(Skip = "Skip due to case sensitive query in EF Core 2.2 in memory provider")]
        public async Task MigrateGroupSource_HasWindowsSource_SuccessAsync()
        {
            var container = _fixture.Bootstrapper.TinyIoCContainer;
            var dbContext = container.Resolve<IAuthorizationDbContext>();
            var groupMigratorService = container.Resolve<GroupMigratorService>();

            var customGroup1 = new Group
            {
                GroupId = Guid.NewGuid(),
                Name = $"Custom Group 1-{Guid.NewGuid()}",
                Source = GroupConstants.CustomSource,
                CreatedBy = "test",
                CreatedDateTimeUtc = DateTime.UtcNow
            };

            var customGroup2 = new Group
            {
                GroupId = Guid.NewGuid(),
                Name = $"Custom Group 2-{Guid.NewGuid()}",
                Source = GroupConstants.CustomSource,
                CreatedBy = "test",
                CreatedDateTimeUtc = DateTime.UtcNow
            };

            var group1 = new Group
            {
                GroupId = Guid.NewGuid(),
                Name = $"Group 1-{Guid.NewGuid()}",
                Source = "windows",
                CreatedBy = "test",
                CreatedDateTimeUtc = DateTime.UtcNow
            };

            var group2 = new Group
            {
                GroupId = Guid.NewGuid(),
                Name = $"Group 2-{Guid.NewGuid()}",
                Source = "Windows",
                CreatedBy = "test",
                CreatedDateTimeUtc = DateTime.UtcNow
            };

            dbContext.Groups.AddRange(new List<Group>
            {
                customGroup1,
                customGroup2,
                group1,
                group2
            });

            dbContext.SaveChanges();

            await groupMigratorService.MigrateWindowsSourceToDirectory();
            await groupMigratorService.MigrateIdentityProvider();

            // group 1
            var browser = _fixture.GetBrowser(Principal, _storageProvider);
            var getResponse = await browser.Get(HttpUtility.UrlEncode($"/groups/{group1.Name}"), with =>
            {
                with.HttpRequest();
            });

            var groupRoleApiModel = getResponse.Body.DeserializeJson<GroupRoleApiModel>();
            Assert.True(groupRoleApiModel.GroupSource == GroupConstants.DirectorySource);
            Assert.True(groupRoleApiModel.IdentityProvider == IdentityConstants.ActiveDirectory);

            // group 2
            getResponse = await browser.Get(HttpUtility.UrlEncode($"/groups/{group2.Name}"), with =>
            {
                with.HttpRequest();
            });

            groupRoleApiModel = getResponse.Body.DeserializeJson<GroupRoleApiModel>();
            Assert.True(groupRoleApiModel.GroupSource == GroupConstants.DirectorySource);
            Assert.True(groupRoleApiModel.IdentityProvider == IdentityConstants.ActiveDirectory);

            // custom group 1
            getResponse = await browser.Get(HttpUtility.UrlEncode($"/groups/{customGroup1.Name}"), with =>
            {
                with.HttpRequest();
            });

            groupRoleApiModel = getResponse.Body.DeserializeJson<GroupRoleApiModel>();
            Assert.True(groupRoleApiModel.GroupSource == GroupConstants.CustomSource);
            Assert.Null(groupRoleApiModel.IdentityProvider);

            // custom group 2
            getResponse = await browser.Get(HttpUtility.UrlEncode($"/groups/{customGroup2.Name}"), with =>
            {
                with.HttpRequest();
            });

            groupRoleApiModel = getResponse.Body.DeserializeJson<GroupRoleApiModel>();
            Assert.True(groupRoleApiModel.GroupSource == GroupConstants.CustomSource);
            Assert.Null(groupRoleApiModel.IdentityProvider);
        }
    }
}
