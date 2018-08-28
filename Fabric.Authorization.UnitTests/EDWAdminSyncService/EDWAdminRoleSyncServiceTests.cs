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
        [Theory, MemberData(nameof(DosAdminUserRoles))]
        public void SyncPermissions_AddsEdwAdminCorrectly(User user)
        {
            var mockEdwStore = new Mock<IEDWStore>();

            var service = new EDWAdminRoleSyncService(mockEdwStore.Object);

            service.RefreshDosAdminRolesAsync(user).Wait();

            mockEdwStore.Verify(mock => mock.RemoveIdentitiesFromRole(It.IsAny<string[]>(), It.IsAny<string>()), Times.Never());
            mockEdwStore.Verify(mock => mock.AddIdentitiesToRole(It.IsAny<string[]>(), It.IsAny<string>()), Times.Once());
        }

        [Theory, MemberData(nameof(NoDosAdminUserRoles))]
        public void SyncPermissions_RemovesEdwAdminCorrectly(User user)
        {
            var mockEdwStore = new Mock<IEDWStore>();

            var service = new EDWAdminRoleSyncService(mockEdwStore.Object);

            service.RefreshDosAdminRolesAsync(user).Wait();

            mockEdwStore.Verify(mock => mock.AddIdentitiesToRole(It.IsAny<string[]>(), It.IsAny<string>()), Times.Never());
            mockEdwStore.Verify(mock => mock.RemoveIdentitiesFromRole(It.IsAny<string[]>(), It.IsAny<string>()), Times.Once());
        }

        public static IEnumerable<object[]> NoDosAdminUserRoles => new[]
        {
            new object[]
            {
                new User("noadminrole", "fabric-authorization")
                {
                    Roles = new Role[] { new Role{ Name = "notadmin" } }
                }
            },
            new object[]
            {
                new User("noadminrole", "fabric-authorization")
                {
                    Groups = new Group[] { new Group { Roles = new Role[] { new Role { Name = "notadmin" } } } }
                }
            },
            new object[]
            {
                new User("noadminrole", "fabric-authorization")
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

        public static IEnumerable<object[]> DosAdminUserRoles => new []
        {
            new object[] 
            {
                new User("dosadminrole", "fabric-authorization")
                {
                    Roles = new Role[] { new Role { Name = "notadmin" }, new Role{ Name = "dosadmin" } }
                }
            },
            new object[] 
            {
                new User("dosadmingroup", "fabric-authorization")
                {
                    Groups = new Group[] { new Group { Roles = new Role[] { new Role { Name = "notadmin" }, new Role { Name = "dosadmin" } } } }
                }
            },
            new object[] 
            {
                new User("dosadminchildgroup", "fabric-authorization")
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
            }
        };
    }
}
