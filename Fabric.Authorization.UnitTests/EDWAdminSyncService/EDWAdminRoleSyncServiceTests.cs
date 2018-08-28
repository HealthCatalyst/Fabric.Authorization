using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores;
using Moq;
using Xunit;

namespace Fabric.Authorization.UnitTests.EDWAdminSyncService
{
    public class EDWAdminRoleSyncServiceTests
    {
        [Theory, MemberData(nameof(SingleUserDosAdminRoles))]
        public void SyncPermissions_AddsEdwAdminCorrectly(User user)
        {
            var mockEdwStore = new Mock<IEDWStore>();

            var service = new EDWAdminRoleSyncService(mockEdwStore.Object);

            service.RefreshDosAdminRolesAsync(user).Wait();

            mockEdwStore.Verify(mock => mock.RemoveIdentitiesFromRole(It.IsAny<string[]>(), It.IsAny<string>()), Times.Never());
            mockEdwStore.Verify(mock => mock.AddIdentitiesToRole(It.IsAny<string[]>(), It.IsAny<string>()), Times.Once());
        }

        [Theory, MemberData(nameof(SingleUserNoDosAdminRoles))]
        public void SyncPermissions_RemovesEdwAdminCorrectly(User user)
        {
            var mockEdwStore = new Mock<IEDWStore>();

            var service = new EDWAdminRoleSyncService(mockEdwStore.Object);

            service.RefreshDosAdminRolesAsync(user).Wait();

            mockEdwStore.Verify(mock => mock.AddIdentitiesToRole(It.IsAny<string[]>(), It.IsAny<string>()), Times.Never());
            mockEdwStore.Verify(mock => mock.RemoveIdentitiesFromRole(It.IsAny<string[]>(), It.IsAny<string>()), Times.Once());
        }

        public static IEnumerable<object[]> SingleUserNoDosAdminRoles => new[]
        {
            new object[]
            {
                new User("noRolesUser", "fabric-authorization") { }
            },
            new object[]
            {
                new User("noAdminRole", "fabric-authorization")
                {
                    Roles = new Role[] { new Role{ Name = "notadmin" } }
                }
            },
            new object[]
            {
                new User("noAdminRole", "fabric-authorization")
                {
                    Groups = new Group[] { new Group { Roles = new Role[] { new Role { Name = "notadmin" } } } }
                }
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
                                    Roles = new Role[] { new Role { Name = "notadmin" } }
                                }
                            }
                        }
                    }
                }
            }
        };

        public static IEnumerable<object[]> SingleUserDosAdminRoles => new []
        {
            new object[] 
            {
                new User("dosAdminRole", "fabric-authorization")
                {
                    Roles = new Role[] { new Role { Name = "notadmin" }, new Role{ Name = "dosadmin" } }
                }
            },
            new object[]
            {
                new User("jobAdminRole", "fabric-authorization")
                {
                    Roles = new Role[] { new Role { Name = "notadmin" }, new Role{ Name = "jobadmin" } }
                }
            },
            new object[]
            {
                new User("datamartAdminRole", "fabric-authorization")
                {
                    Roles = new Role[] { new Role { Name = "notadmin" }, new Role{ Name = "datamartadmin" } }
                }
            },
            new object[] 
            {
                new User("dosAdminGroup", "fabric-authorization")
                {
                    Groups = new Group[] { new Group { Roles = new Role[] { new Role { Name = "notadmin" }, new Role { Name = "dosadmin" } } } }
                }
            },
            new object[]
            {
                new User("jobAdminGroup", "fabric-authorization")
                {
                    Groups = new Group[] { new Group { Roles = new Role[] { new Role { Name = "notadmin" }, new Role { Name = "jobadmin" } } } }
                }
            },
            new object[]
            {
                new User("datamartAdminGroup", "fabric-authorization")
                {
                    Groups = new Group[] { new Group { Roles = new Role[] { new Role { Name = "notadmin" }, new Role { Name = "datamartadmin" } } } }
                }
            },
            new object[] 
            {
                new User("dosAdminChildGroup", "fabric-authorization")
                {
                    Groups = new Group[] 
                    {
                        new Group
                        {
                            Children = new Group[]  
                            {
                                new Group
                                {
                                    Roles = new Role[] { new Role { Name = "notadmin" }, new Role { Name = "dosadmin" } }
                                }
                            }
                        }
                    }
                }
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
                                    Roles = new Role[] { new Role { Name = "notadmin" }, new Role { Name = "jobadmin" } }
                                }
                            }
                        }
                    }
                }
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
                                    Roles = new Role[] { new Role { Name = "notadmin" }, new Role { Name = "datamartadmin" } }
                                }
                            }
                        }
                    }
                }
            }
        };
    }
}
