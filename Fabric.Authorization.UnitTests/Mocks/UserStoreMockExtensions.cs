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
    public static class UserStoreMockExtensions
    {
        public static Mock<IUserStore> SetupGetUser(this Mock<IUserStore> mockUserStore, List<User> users)
        {
            mockUserStore.Setup(userStore => userStore.Get(It.IsAny<string>()))
                .Returns((string userId) =>
                {
                    if (users.Any(c => c.Id == userId))
                    {
                        return Task.FromResult(users.First(c => c.Id == userId));
                    }
                    throw new NotFoundException<User>();
                });

            mockUserStore.Setup(userStore => userStore.Exists(It.IsAny<string>()))
                .Returns((string userId) =>
                {
                    var delimiter = new[] { @":" };
                    var idParts = userId.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
                    return Task.FromResult(users.Any(c => c.SubjectId == idParts[1] && c.IdentityProvider == idParts[0]));
                });

            mockUserStore.Setup(userStore => userStore.GetAll())
                .Returns(() => Task.FromResult(users.AsEnumerable()));

            mockUserStore.Setup(userStore => userStore.Add(It.IsAny<User>()))
                .Returns((User user) =>
                {
                    users.Add(user);
                    return Task.FromResult(user);
                });

            return mockUserStore;
        }
    }
}