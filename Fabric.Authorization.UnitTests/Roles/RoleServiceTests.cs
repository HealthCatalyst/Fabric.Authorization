using System;
using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.UnitTests.Mocks;
using Moq;
using Xunit;

namespace Fabric.Authorization.UnitTests.Roles
{
    public class RoleServiceTests
    {
        [Theory]
        [MemberData(nameof(IncompatiblePermissionData))]
        public void AddPermissionToRole_ThrowsIncompatiblePermissionException(Role existingRole,
            Permission existingPermission, Permission permissionToAdd)
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
                .SetupGetRoles(new List<Role> {existingRole})
                .Create();

            var roleService = new RoleService(mockRoleStore, mockPermissionStore);
            Assert.Throws<AggregateException>(() => roleService
                .AddPermissionsToRole(existingRole, new[] {permissionToAdd.Id}, new Guid[]{}).Result);
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
                }
            };
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
                .SetupGetPermissions(new List<Permission> {permissionToRemove})
                .Create();

            var existingRole = new Role
            {
                Id = Guid.NewGuid(),
                Grain = "app",
                SecurableItem = "patientsafety",
                Name = "admin"
            };
            var mockRoleStore = new Mock<IRoleStore>()
                .SetupGetRoles(new List<Role> {existingRole})
                .Create();


            var roleService = new RoleService(mockRoleStore, mockPermissionStore);
            Assert.Throws<AggregateException>(() => roleService
                .RemovePermissionsFromRole(existingRole, new[] {permissionToRemove.Id}).Result);
        }
    }
}