using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Validators;
using Fabric.Authorization.UnitTests.Mocks;
using Moq;
using Xunit;

namespace Fabric.Authorization.UnitTests.Roles
{
    public class RoleValidatorTests
    {
        [Theory]
        [MemberData(nameof(RoleRequestData))]
        public void RoleValidator_ValidateRole_ReturnsInvalidIfModelNotValid(string grain, string securableItem,
            string roleName, int errorCount)
        {
            var existingRole = new Role
            {
                Grain = "app",
                SecurableItem = "patientsafety",
                Name = "admin"
            };

            var mockRoleStore = new Mock<IRoleStore>()
                .SetupGetRoles(new List<Role> {existingRole}).Create();
            var mockPermissionStore = new Mock<IPermissionStore>().Create();
            var roleValidator = new RoleValidator(new RoleService(mockRoleStore, mockPermissionStore));
            var validationResult = roleValidator.Validate(new Role
            {
                Grain = grain,
                SecurableItem = securableItem,
                Name = roleName
            });

            Assert.False(validationResult.IsValid);
            Assert.NotNull(validationResult.Errors);
            Assert.Equal(errorCount, validationResult.Errors.Count);
        }

        public static IEnumerable<object[]> RoleRequestData => new[]
        {
            new object[] {"app", "patientsafety", "", 1},
            new object[] {"app", "", "", 2},
            new object[] {"", "", "", 3},
            new object[] {"app", "patientsafety", null, 1},
            new object[] {"app", null, null, 2},
            new object[] {null, null, null, 3},
            new object[] {"app", "patientsafety", "admin", 1}
        };

        [Fact]
        public void RoleValidator_ValidateRole_ReturnsValid()
        {
            var mockRoleStore = new Mock<IRoleStore>().SetupGetRoles(new List<Role>()).Create();
            var mockPermissionStore = new Mock<IPermissionStore>().Create();
            var roleValidator = new RoleValidator(new RoleService(mockRoleStore, mockPermissionStore));
            var validationResult = roleValidator.Validate(new Role
            {
                Grain = "app",
                SecurableItem = "patientsafety",
                Name = "admin"
            });

            Assert.True(validationResult.IsValid);
        }

        [Fact]
        public void RoleValidator_ValidateRole_UniqueCheckIgnoresDeleted()
        {
            var grain = "app";
            var securableItem = "patientsafety";
            var name = "admin";

            var existingRole = new Role
            {
                Grain = grain,
                SecurableItem = securableItem,
                Name = name,
                IsDeleted = true
            };

            var mockRoleStore = new Mock<IRoleStore>()
                .SetupGetRoles(new List<Role> {existingRole}).Create();
            var mockPermissionStore = new Mock<IPermissionStore>().Create();
            var roleValidator = new RoleValidator(new RoleService(mockRoleStore, mockPermissionStore));
            var validationResult = roleValidator.Validate(new Role
            {
                Grain = grain,
                SecurableItem = securableItem,
                Name = name
            });

            Assert.True(validationResult.IsValid);
        }
    }
}