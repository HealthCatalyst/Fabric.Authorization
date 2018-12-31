namespace Fabric.Authorization.UnitTests.EDWAdminSyncService
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Fabric.Authorization.Domain.Models;
    using Fabric.Authorization.Domain.Services;
    using Fabric.Authorization.Domain.Stores;
    using Moq;
    using Xunit;

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

        [Theory, MemberData(nameof(MultipleUsers))]
        public async Task SyncPermissions_ProcessesMultipleUserEdwAdminCorrectlyAsync(IEnumerable<User> user, int numberAdded, int numberRemoved)
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

        [Fact]
        public async Task SyncPermissions_NullUserDoesNotThrowsExceptionAsync()
        {
            // Arrange
            User user = null;
            var mockEdwStore = new Mock<IEDWStore>();
            var service = new EDWAdminRoleSyncService(mockEdwStore.Object);

            // Act
            Exception result = null;
            try
            {
                await service.RefreshDosAdminRolesAsync(user);
            }
            catch (Exception exc)
            {
                result = exc;
            }

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task SyncPermissions_NullUserArrayDoesNotThrowsExceptionAsync()
        {
            // Arrange
            IEnumerable<User> user = null;
            var mockEdwStore = new Mock<IEDWStore>();
            var service = new EDWAdminRoleSyncService(mockEdwStore.Object);

            // Act
            Exception result = null;
            try
            {
                await service.RefreshDosAdminRolesAsync(user);
            }
            catch (Exception exc)
            {
                result = exc;
            }

            // Assert
            Assert.Null(result);
        }
        
        public static Group DosAdminsGroup => new Group()
        {
            Name = "DosAdmins",
            Roles = new Role[] { DataMartAdminRole }
        };

        public static Group DosAdminsDeletedGroup => new Group()
        {
            Name = "DosAdmins",
            IsDeleted = true,
            Roles = new Role[] { DataMartAdminRole }
        };

        public static Group DatamartAdminGroup => new Group()
        {
            Name = "AdminGroup",
            Roles = new Role[] { DataMartAdminRole },
        };

        public static Group JobAdminGroup => new Group()
        {
            Name = "AdminGroup",
            Roles = new Role[] { JobAdminRole },

        };

        public static Group ParentGroup => new Group()
        {
            Name = "ParentGroup",
            Roles = new Role[] { NoAdminRole },
            Children = new List<Group>()
            {
                DatamartAdminGroup
            }
        };

        public static Group NoAdminGroupWithAdminParent => new Group()
        {
            Name = "ParentInheirtence",
            Roles = new Role[] { NoAdminRole },
            Parents = new List<Group>()
            {
                DosAdminsGroup
            }
        };

        public static Role NoAdminRole => new Role() { Name = "NoAdminRole" };

        public static Role JobAdminRole => new Role() { Name = "jobadmin" };

        public static Role DataMartAdminRole => new Role() { Name = "datamartadmin" };

        /// <summary>
        /// Declares the member data for the single user tests
        /// </summary>
        public static IEnumerable<object[]> SingleUsers => new[]
        {
            new object[] // if user is not in DosAdmins group, it should be removed from EdwAdmin
            {
                new User("testSubjectId1", "windows")
                {
                    Roles = new Role[] { NoAdminRole }
                },
                0,
                1
            },
            new object[] // if user is dos admin, it should be removed from EdwAdmin
            {
                new User("testSubjectId2", "windows")
                {
                    Roles = new Role[] { new Role{ Name = "dosadmin" } }
                },
                0,
                1
            },
            new object[] // if user is DatamartAdmin but not in DosAdmins, it should be removed from EdwAdmin
            {
                new User("testSubjectId3", "windows")
                {
                    Roles = new Role[] { DataMartAdminRole }
                },
                0,
                1
            },
            new object[]  // if user is job admin but not in DosAdmins, it should be removed from EdwAdmin
            {
                new User("testSubjectId4", "windows")
                {
                    Roles = new Role[] { JobAdminRole }
                },
                0,
                1
            },
            new object[]  // if user is in DosAdmins, then it should be added to EdwAdmin
            {
                new User("testSubjectId5", "windows")
                {
                    Groups = new Group[] { DosAdminsGroup }
                },
                1,
                0
            },
            new object[]  // if user is deleted, it should be removed from EdwAdmin
            {
                new User("testSubjectId6", "windows")
                {
                    Groups = new Group[] { DosAdminsGroup },
                    IsDeleted = true
                },
                0,
                1
            },
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
                0,
                1
            },
            new object[]
            {
                new User("datamartAdminRole", "fabric-authorization")
                {
                    Roles = new Role[] { NoAdminRole, DataMartAdminRole }
                },
                0,
                1
            },
            new object[]
            {
                new User("datamartAdminJobAdminAdminRole", "fabric-authorization")
                {
                    Roles = new Role[] { JobAdminRole, DataMartAdminRole }
                },
                0,
                1
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
                0,
                1
            },
            new object[]
            {
                new User("jobAdminGroup", "fabric-authorization")
                {
                    Groups = new Group[] { new Group { Roles = new Role[] { NoAdminRole, JobAdminRole } } }
                },
                0,
                1
            },
            new object[]
            {
                new User("datamartAdminGroup", "fabric-authorization")
                {
                    Groups = new Group[] { new Group { Roles = new Role[] { NoAdminRole, DataMartAdminRole } } }
                },
                0,
                1
            },
            new object[]
            {
                new User("dosAdminsGroupDeleted", "fabric-authorization")
                {
                    Groups = new Group[] { DosAdminsDeletedGroup }
                },
                0,
                1
            }, 
        };

        public static IEnumerable<object[]> MultipleUsers => new[]
        {
            new object[] // if one user is in DosAdmins and one isn't, then should be a remove and add
            {
                new List<User>() {
                    new User("testSubjectId6", "windows")
                    {
                        Roles = new Role[] { NoAdminRole },
                        Groups = new Group[] { DatamartAdminGroup }
                    },
                    new User("testSubjectId7", "windows")
                    {
                        Roles = new Role[] { DataMartAdminRole },
                        Groups = new Group[] { DosAdminsGroup }
                    }
                },
                1,
                1
            },
            new object[] // If a group is deleted, then that group should be removed from edwAdmin
            {
                new List<User>() {
                    new User("testSubjectId8", "windows")
                    {
                        Roles = new Role[] { NoAdminRole },
                        Groups = new List<Group>() { DosAdminsDeletedGroup }
                    },
                    new User("testSubjectId9", "windows")
                    {
                        Roles = new Role[] { NoAdminRole },
                        Groups = new List<Group>() { DosAdminsDeletedGroup }
                    }
                },
                0,
                2
            },
            new object[] // If DosAdmins group is deleted, then remove all users (including admins)
            {
                new List<User>() {
                    new User("testSubjectId10", "windows")
                    { // gets removed from the group
                        Roles = new Role[] { NoAdminRole },
                        Groups = new List<Group>() { DosAdminsDeletedGroup }
                    },
                    new User("testSubjectId11", "windows")
                    { // stays in the group
                        Roles = new Role[] { JobAdminRole },
                        Groups = new List<Group>() { DosAdminsDeletedGroup }
                    }
                },
                0,
                2
            },
            new object[] // If a child group has admin role, then the parent roles should not change
            {
                new List<User>() {
                    new User("testSubjectId12", "windows")
                    { // does not get added
                        Roles = new Role[] { new Role { Name = "notadmin" } },
                        Groups = new List<Group>() { ParentGroup }
                    },
                    new User("testSubjectId13", "windows")
                    { // does not get added
                        Roles = new Role[] { new Role { Name = "notadmin" } },
                        Groups = new List<Group>() { ParentGroup }
                    }
                },
                0,
                2
            },
            new object[] // If non-DosAdmins group has admin role, then do not add edwadmin
            {
                new List<User>() {
                    new User("testSubjectId12", "windows")
                    { // gets added to admin group
                        Roles = new Role[] { new Role { Name = "notadmin" } },
                        Groups = new List<Group>() { JobAdminGroup }
                    },
                    new User("testSubjectId13", "windows")
                    { // gets added to admin group
                        Roles = new Role[] { new Role { Name = "notadmin" } },
                        Groups = new List<Group>() { JobAdminGroup }
                    }
                },
                0,
                2
            },
            new object[] // if users are in a child group of DosAdmins but not in DosAdmins,
                         // then they should be removed from EdwAdmin
            {
                new List<User>() {
                    new User("testSubjectId18", "windows")
                    { // add to edwadmin
                        Roles = new Role[] { new Role { Name = "noadmin" } },
                        Groups = new List<Group>() { NoAdminGroupWithAdminParent }
                    },
                    new User("testSubjectId19", "windows")
                    { // add to edwadmin
                        Roles = new Role[] { new Role { Name = "noadmin" } },
                        Groups = new List<Group>() { NoAdminGroupWithAdminParent }
                    }
                },
                0,
                2
            }
        };
    }
}
