using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Services;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Persistence.SqlServer.Configuration;
using Fabric.Authorization.Persistence.SqlServer.EntityModels;
using Fabric.Authorization.Persistence.SqlServer.Services;
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
            _fixture = fixture;
            _storageProvider = storageProvider;
        }

        [Fact]
        public async Task MigrateDuplicateGroups_NoDuplicates_Success()
        {
            var container = _fixture.Bootstrapper.TinyIoCContainer;
            var dbContext = container.Resolve<IAuthorizationDbContext>();
            var store = container.Resolve<IGroupStore>();
            var groupMigratorService = container.Resolve<GroupMigratorService>();

            var customGroup1 = new Group
            {
                GroupId = Guid.NewGuid(),
                Name = "Custom Group 1",
                Source = GroupConstants.CustomSource,
                CreatedBy = "test",
                CreatedDateTimeUtc = DateTime.UtcNow
            };

            var customGroup2 = new Group
            {
                GroupId = Guid.NewGuid(),
                Name = "Custom Group 2",
                Source = GroupConstants.CustomSource,
                CreatedBy = "test",
                CreatedDateTimeUtc = DateTime.UtcNow
            };

            var group1 = new Group
            {
                GroupId = Guid.NewGuid(),
                Name = "Group 1",
                Source = GroupConstants.DirectorySource,
                CreatedBy = "test",
                CreatedDateTimeUtc = DateTime.UtcNow
            };

            var group2 = new Group
            {
                GroupId = Guid.NewGuid(),
                Name = "Group 2",
                Source = GroupConstants.DirectorySource,
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
        }

        [Fact]
        public async Task MigrateDuplicateDirectoryGroups_DuplicateNames_Success()
        {
            var container = _fixture.Bootstrapper.TinyIoCContainer;
            var dbContext = container.Resolve<IAuthorizationDbContext>();
            var groupMigratorService = container.Resolve<GroupMigratorService>();

            #region Data Setup

            var client = new Client
            {
                ClientId = "client1",
                Name = "Client 1"
            };

            var grain = new Grain
            {
                Name = "dos"
            };

            var securableItem = new SecurableItem
            {
                Name = "datamarts",
                Grain = grain,
                ClientOwner = client.ClientId
            };

            var customGroup1 = new Group
            {
                GroupId = Guid.NewGuid(),
                Name = "Custom Group 1",
                Source = GroupConstants.CustomSource,
                CreatedBy = "test",
                CreatedDateTimeUtc = DateTime.UtcNow
            };

            var customGroup2 = new Group
            {
                GroupId = Guid.NewGuid(),
                Name = "Custom Group 2",
                Source = GroupConstants.CustomSource,
                CreatedBy = "test",
                CreatedDateTimeUtc = DateTime.UtcNow
            };

            var group1 = new Group
            {
                GroupId = Guid.NewGuid(),
                Name = "Group 1",
                Source = GroupConstants.DirectorySource,
                CreatedBy = "test",
                CreatedDateTimeUtc = DateTime.UtcNow
            };

            var group2 = new Group
            {
                GroupId = Guid.NewGuid(),
                Name = "groUP 1",
                Source = GroupConstants.DirectorySource,
                CreatedBy = "test",
                CreatedDateTimeUtc = DateTime.UtcNow
            };

            var role1 = new Role
            {
                RoleId = Guid.NewGuid(),
                Name = "Role 1",
                Grain = "dos",
                SecurableItem = securableItem
            };

            var role2 = new Role
            {
                RoleId = Guid.NewGuid(),
                Name = "Role 2",
                Grain = "dos",
                SecurableItem = securableItem
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

            var user1 = new User
            {
                IdentityProvider = "windows",
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
                role2
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
                group2Role2
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
        }

        [Fact]
        public async Task MigrateDuplicateCustomGroups_DuplicateNames_Success()
        {
            
        }

        [Fact]
        public void MigrateDuplicateGroups_HasDuplicateIdentifiers_Success()
        {
            // TODO: write test for this once merged to master with GroupIdentifier logic
        }
    }
}
