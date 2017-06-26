using System;
using System.Collections.Generic;
using System.Linq;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.UnitTests.Mocks;
using Moq;
using Xunit;

namespace Fabric.Authorization.UnitTests.Roles
{
    public class RoleServiceTests
    {
        [Fact]
        public void AddPermissionToRole_Succeeds()
        {
            var existingRole = new Role
            {
                Id = Guid.NewGuid(),
                Grain = "app",
                SecurableItem = "patientsafety",
                Name = "admin"
            };
            var mockRoleStore = new Mock<IRoleStore>()
                .SetupGetRoles(new List<Role> { existingRole })
                .Create();

            var permissionToAdd = new Permission
            {
                Id = Guid.NewGuid(),
                Grain = "app",
                SecurableItem = "patientsafety",
                Name = "manageusers"
            };
            var mockPermissionStore = new Mock<IPermissionStore>()
                .SetupGetPermissions(new List<Permission> { permissionToAdd })
                .Create();
            var roleService = new RoleService(mockRoleStore, mockPermissionStore);
            var updatedRole = roleService.AddPermissionsToRole(existingRole, new Guid[] { permissionToAdd.Id });
            Assert.Equal(1, updatedRole.Permissions.Count);
            Assert.Equal(permissionToAdd.Id, updatedRole.Permissions.First().Id);
        }

        [Theory, MemberData(nameof(IncompatiblePermissionData))]
        public void AddPermissionToRole_ThrowsIncompatiblePermissionException(Role existingRole, Permission existingPermission, Permission permissionToAdd)
        {
            var permissions = new List<Permission>();
            if (existingPermission != null)
            {
                permissions.Add(existingPermission);
                existingRole.Permissions.Add(existingPermission);
            }
            permissions.Add(permissionToAdd);

            var mockPermissionStore = new Mock<IPermissionStore>()
                .SetupGetPermissions(permissions)
                .Create();

            var mockRoleStore = new Mock<IRoleStore>()
            .SetupGetRoles(new List<Role> { existingRole })
            .Create();
            
            var roleService = new RoleService(mockRoleStore, mockPermissionStore);
            Assert.Throws<IncompatiblePermissionException>(() => roleService.AddPermissionsToRole(existingRole, new Guid[] { permissionToAdd.Id }));
        }

        [Fact]
        public void RemovePermissionFromRole_Succeeds()
        {
            var permissionToRemove = new Permission
            {
                Id = Guid.NewGuid(),
                Grain = "app",
                SecurableItem = "patientsafety",
                Name = "manageusers"
            };
            var mockPermissionStore = new Mock<IPermissionStore>()
                .SetupGetPermissions(new List<Permission> { permissionToRemove })
                .Create();

            var existingRole = new Role
            {
                Id = Guid.NewGuid(),
                Grain = "app",
                SecurableItem = "patientsafety",
                Name = "admin",
                Permissions = new List<Permission> { permissionToRemove }
            };
            var mockRoleStore = new Mock<IRoleStore>()
                .SetupGetRoles(new List<Role> { existingRole })
                .Create();

            
            var roleService = new RoleService(mockRoleStore, mockPermissionStore);
            var updatedRole = roleService.RemovePermissionsFromRole(existingRole, new []{permissionToRemove.Id});
            Assert.False(updatedRole.Permissions.Any());

        }

        [Fact]
        public void RemovePermissionFromRole_ThrowsPermissionNotFoundException()
        {
            var permissionToRemove = new Permission
            {
                Id = Guid.NewGuid(),
                Grain = "app",
                SecurableItem = "patientsafety",
                Name = "manageusers"
            };
            var mockPermissionStore = new Mock<IPermissionStore>()
                .SetupGetPermissions(new List<Permission> { permissionToRemove })
                .Create();

            var existingRole = new Role
            {
                Id = Guid.NewGuid(),
                Grain = "app",
                SecurableItem = "patientsafety",
                Name = "admin",
            };
            var mockRoleStore = new Mock<IRoleStore>()
                .SetupGetRoles(new List<Role> { existingRole })
                .Create();


            var roleService = new RoleService(mockRoleStore, mockPermissionStore);
            Assert.Throws<PermissionNotFoundException>(() => roleService.RemovePermissionsFromRole(existingRole, new[] { permissionToRemove.Id }));
        }

        public static IEnumerable<object[]> IncompatiblePermissionData()
        {
            var existingPermissionGuid = Guid.NewGuid();
            return new[]
            {
                new object[]
                {
                    new Role {Id = Guid.NewGuid(), Grain = "app", SecurableItem = "patientsafety", Name = "admin"},
                    null,
                    new Permission {Id = Guid.NewGuid(), Grain = "app", SecurableItem = "idea", Name = "manageusers"}
                },
                new object[]
                {
                    new Role {Id = Guid.NewGuid(), Grain = "app", SecurableItem = "patientsafety", Name = "admin"},
                    null,
                    new Permission
                    {
                        Id = Guid.NewGuid(),
                        Grain = "data",
                        SecurableItem = "patientsafety",
                        Name = "manageusers"
                    }
                },
                new object[]
                {
                    new Role {Id = Guid.NewGuid(), Grain = "app", SecurableItem = "patientsafety", Name = "admin"},
                    new Permission
                    {
                        Id = existingPermissionGuid,
                        Grain = "app",
                        SecurableItem = "patientsafety",
                        Name = "manageusers"
                    },
                    new Permission
                    {
                        Id = existingPermissionGuid,
                        Grain = "app",
                        SecurableItem = "patientsafety",
                        Name = "manageusers"
                    }
                },
            };
        } 
    }
}
