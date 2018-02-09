using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;
using Moq;

namespace Fabric.Authorization.UnitTests.Mocks
{
    public static class RoleStoreMockExtensions
    {
        public static Mock<IRoleStore> SetupGetRoles(this Mock<IRoleStore> mockRoleStore, List<Role> roles)
        {
            mockRoleStore
                .Setup(roleStore => roleStore.GetRoles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string grain, string securableItem, string name) =>
                {
                    if (!string.IsNullOrEmpty(grain))
                    {
                        roles = roles.Where(r => r.Grain == grain).ToList();
                    }
                    if (!string.IsNullOrEmpty(securableItem))
                    {
                        roles = roles.Where(r => r.SecurableItem == securableItem).ToList();
                    }
                    if (!string.IsNullOrEmpty(name))
                    {
                        roles = roles.Where(r => r.Name == name).ToList();
                    }

                    return Task.FromResult(roles.Where(r => !r.IsDeleted));
                });

            return mockRoleStore.SetupGetRole(roles.ToList());
        }

        public static Mock<IRoleStore> SetupGetRole(this Mock<IRoleStore> mockRoleStore, List<Role> roles)
        {
            mockRoleStore.Setup(roleStore => roleStore.Get(It.IsAny<Guid>()))
                .Returns((Guid roleId) =>
                {
                    if (roles.Any(r => r.Id == roleId))
                    {
                        return Task.FromResult(roles.First(r => r.Id == roleId));
                    }
                    throw new NotFoundException<Role>();
                });

            return mockRoleStore;
        }

        public static Mock<IRoleStore> SetupAddRole(this Mock<IRoleStore> mockRoleStore, List<Role> roles)
        {
            mockRoleStore.Setup(roleStore => roleStore.Add(It.IsAny<Role>()))
                .Returns((Role r) =>
                {
                    r.Id = Guid.NewGuid();
                    roles.Add(r);
                    return Task.FromResult(r);
                });
            return mockRoleStore;
        }

        public static IRoleStore Create(this Mock<IRoleStore> mockRoleStore)
        {
            return mockRoleStore.Object;
        }
    }
}