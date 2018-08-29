using System.Collections.Generic;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores;
using Moq;
using Xunit;

namespace Fabric.Authorization.UnitTests.EDWAdminSyncService
{
    public class EDWAdminRoleSyncServiceTests
    {
        [Theory, MemberData(nameof(SingleUsers))]
        public async Task SyncPermissions_ProcessesSingleUserEdwAdminCorrectlyAsync(User user, int numberAdded, int numberRemoved)
        {
            // Arrange
            var mockEdwStore = new Mock<IEDWStore>();
            var service = new EDWAdminRoleSyncService(mockEdwStore.Object);

            // Act
            await service.RefreshDosAdminRolesAsync(user);

            // Assert
            mockEdwStore.Verify(mock => mock.AddIdentitiesToRole(It.IsAny<string[]>(), It.IsAny<string>()), Times.Exactly(numberAdded));
            mockEdwStore.Verify(mock => mock.RemoveIdentitiesFromRole(It.IsAny<string[]>(), It.IsAny<string>()), Times.Exactly(numberRemoved));
        }

        public static Role NoAdminRole => new Role() { Name = "NoAdminRole" };

        public static Role JobAdminRole => new Role() { Name = "jobadmin" };

        public static Role DataMartAdminRole => new Role() { Name = "datamartadmin" };

        public static IEnumerable<object[]> SingleUsers => new[] 
        {
            new object[]
            {
                new User("noRolesUser", "fabric-authorization") { },
                0,
                1
            },
            new object[]
            {
                new User("noAdminRole", "fabric-authorization")
                {
                    Roles = new Role[] { NoAdminRole }
                },
                0,
                1
            },
            new object[]
            {
                new User("noAdminRole", "fabric-authorization")
                {
                    Groups = new Group[] { new Group { Roles = new Role[] { NoAdminRole } } }
                },
                0,
                1
            },
            new object[]
            {
                new User("noAdminRole", "fabric-authorization")
                {
                    Groups = new Group[]
                    {
                        new Group
                        {
                            Children = new Group[]
                            {
                                new Group
                                {
                                    Roles = new Role[] { NoAdminRole }
                                }
                            }
                        }
                    }
                },
                0,
                1
            },
            new object[]
            {
                new User("jobAdminRole", "fabric-authorization")
                {
                    Roles = new Role[] { NoAdminRole, JobAdminRole }
                },
                1,
                0
            },
            new object[]
            {
                new User("datamartAdminRole", "fabric-authorization")
                {
                    Roles = new Role[] { NoAdminRole, DataMartAdminRole }
                },
                1,
                0
            },
            new object[]
            {
                new User("datamartAdminJobAdminAdminRole", "fabric-authorization")
                {
                    Roles = new Role[] { JobAdminRole, DataMartAdminRole }
                },
                1,
                0
            },
            new object[]
            {
                new User("adminRoleIsDeleted", "fabric-authorization")
                {
                    Roles = new Role[] {
                        new Role()
                        {
                            Name = "jobadmin",
                            IsDeleted = true
                        }
                    }
                },
                0,
                1
            },
            new object[]
            {
                new User("twoAdminRolesOneIsDeleted", "fabric-authorization")
                {
                    Roles = new Role[] {
                        new Role()
                        {
                            Name = "jobadmin",
                            IsDeleted = true
                        },
                        DataMartAdminRole
                    }
                },
                1,
                0
            },
            new object[]
            {
                new User("jobAdminGroup", "fabric-authorization")
                {
                    Groups = new Group[] { new Group { Roles = new Role[] { NoAdminRole, JobAdminRole } } }
                },
                1,
                0
            },
            new object[]
            {
                new User("datamartAdminGroup", "fabric-authorization")
                {
                    Groups = new Group[] { new Group { Roles = new Role[] { NoAdminRole, DataMartAdminRole } } }
                },
                1,
                0
            },
            new object[]
            {
                new User("jobAdminChildGroup", "fabric-authorization")
                {
                    Groups = new Group[]
                    {
                        new Group
                        {
                            Children = new Group[]
                            {
                                new Group
                                {
                                    Roles = new Role[] { NoAdminRole, JobAdminRole }
                                }
                            }
                        }
                    }
                },
                1,
                0
            },
            new object[]
            {
                new User("datamartAdminChildGroup", "fabric-authorization")
                {
                    Groups = new Group[]
                    {
                        new Group
                        {
                            Children = new Group[]
                            {
                                new Group
                                {
                                    Roles = new Role[] { NoAdminRole, DataMartAdminRole }
                                }
                            }
                        }
                    }
                },
                1,
                0
            }
        };
    }
}
