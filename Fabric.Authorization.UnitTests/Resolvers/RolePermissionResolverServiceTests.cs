using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Resolvers.Models;
using Fabric.Authorization.Domain.Resolvers.Permissions;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.UnitTests.Mocks;
using Moq;
using Xunit;

namespace Fabric.Authorization.UnitTests.Resolvers
{
    public class RolePermissionResolverServiceTests
    {
        [Fact]
        public async Task RolePermissionResolver_GroupWithNameOnly_SuccessAsync()
        {
            var securableItem = "testapp";
            var subjectId = "testuser";
            var groups = new List<Group>
            {
                new Group
                {
                    Name = "contributor"
                }
            };

            var roles = CreateRoles(securableItem, subjectId, groups);
            var mockPermissionStore = new Mock<IPermissionStore>().Object;
            var mockRoleStore = new Mock<IRoleStore>()
                .SetupGetRoles(roles)
                .Object;
            var roleService = new RoleService(mockRoleStore, mockPermissionStore);
            var resolverService = new RolePermissionResolverService(roleService);
            var resolutionResult = await resolverService.Resolve(new PermissionResolutionRequest
            {
                Grain = Domain.Defaults.Authorization.AppGrain,
                IdentityProvider = "Windows",
                SecurableItem = securableItem,
                SubjectId = subjectId,
                UserGroups = groups,
                IncludeSharedPermissions = false
            });
            Assert.Equal(2, resolutionResult.AllowedPermissions.Count());
        }

        [Theory]
        [InlineData(null, null, 1)]
        [InlineData("Windows", null, 1)]
        [InlineData("Windows", "Tenant123", 2)]
        public async Task RolePermissionResolver_DuplicateGroupNames_SuccessAsync(string groupIdentityProvider, string tenantId, int expectedPermissions)
        {
            var securableItem = "testapp";
            var subjectId = "testuser";
            var groups = new List<Group>
            {
                new Group
                {
                    Name = "contributor",
                    IdentityProvider = "Windows",
                    TenantId = "Tenant123"
                },
                new Group
                {
                    Name = "contributor",
                    IdentityProvider = "Azure",
                    TenantId = "Tenant456"
                }
            };

            var roles = CreateRoles(securableItem, subjectId, groups);
            var mockPermissionStore = new Mock<IPermissionStore>().Object;
            var mockRoleStore = new Mock<IRoleStore>()
                .SetupGetRoles(roles)
                .Object;
            var roleService = new RoleService(mockRoleStore, mockPermissionStore);
            var resolverService = new RolePermissionResolverService(roleService);
            var resolutionResult = await resolverService.Resolve(new PermissionResolutionRequest
            {
                Grain = Domain.Defaults.Authorization.AppGrain,
                IdentityProvider = "Windows",
                SecurableItem = securableItem,
                SubjectId = subjectId,
                UserGroups = new List<Group> { new Group {IdentityProvider = groupIdentityProvider, TenantId = tenantId, Name = "contributor"}},
                IncludeSharedPermissions = false
            });
            Assert.Equal(expectedPermissions, resolutionResult.AllowedPermissions.Count());
        }

        private List<Role> CreateRoles(string securableItem, string subjectId, IList<Group> groups)
        {
            var roles = new List<Role>
            {
                new Role
                {
                    Id = Guid.NewGuid(),
                    Name = "contributor",
                    SecurableItem = securableItem,
                    Grain = Domain.Defaults.Authorization.AppGrain,
                    Groups = groups,
                    Permissions = new List<Permission>
                    {
                        new Permission
                        {
                            Id = Guid.NewGuid(),
                            SecurableItem = securableItem,
                            Grain = Domain.Defaults.Authorization.AppGrain,
                            Name = "edit"
                        }
                    }
                },
                new Role
                {
                    Id = Guid.NewGuid(),
                    Name = "read",
                    SecurableItem = securableItem,
                    Grain = Domain.Defaults.Authorization.AppGrain,
                    Users = new List<User>
                    {
                        new User(subjectId, "Windows")
                    },
                    Permissions = new List<Permission>
                    {
                        new Permission
                        {
                            Id = Guid.NewGuid(),
                            SecurableItem = securableItem,
                            Grain = Domain.Defaults.Authorization.AppGrain,
                            Name = "read"
                        }
                    }
                },
                new Role
                {
                    Id = Guid.NewGuid(),
                    Name = "admin",
                    SecurableItem = securableItem,
                    Grain = Domain.Defaults.Authorization.AppGrain,
                    Users = new List<User>
                    {
                        new User(subjectId + Guid.NewGuid(), "Windows")
                    },
                    Permissions = new List<Permission>
                    {
                        new Permission
                        {
                            Id = Guid.NewGuid(),
                            SecurableItem = securableItem,
                            Grain = Domain.Defaults.Authorization.AppGrain,
                            Name = "delete"
                        }
                    }
                },
                new Role
                {
                    Id = Guid.NewGuid(),
                    Name = "owner",
                    SecurableItem = securableItem,
                    Grain = Domain.Defaults.Authorization.AppGrain,
                    Groups = new List<Group>
                    {
                        new Group
                        {
                            Name = "admins"
                        }
                    }, 
                    Permissions = new List<Permission>
                    {
                        new Permission
                        {
                            Id = Guid.NewGuid(),
                            SecurableItem = securableItem,
                            Grain = Domain.Defaults.Authorization.AppGrain,
                            Name = "takeownership"
                        }
                    }
                }
            };
            return roles;
        }
    }
}
