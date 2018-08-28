using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores;
using Moq;
using Xunit;

namespace Fabric.Authorization.UnitTests.EDWAdminSyncService
{
    public class EDWAdminRoleSyncServiceTests_2
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

        public static Group DeletedGroup => new Group()
        {
            Name = "DeletedGroup",
            IsDeleted = true,
            Roles = new Role[] { new Role { Name = "datamartadmin" } }
        };

        public static Group DatamartAdmin => new Group()
        {
            Name = "AdminGroup",
            Roles = new Role[] { new Role { Name = "datamartadmin" } },
        };

        public static Group JobAdminGroup => new Group()
        {
            Name = "AdminGroup",
            Roles = new Role[] { new Role { Name = "jobadmin" } },
        };

        public static Group ParentGroup => new Group()
        {
            Name = "ParentGroup",
            Roles = new Role[] { new Role { Name = "noadmin" } },
            Children = new List<Group>()
            {
                DatamartAdmin
            }
        };

        public static Group DeletedChildGroup => new Group()
        {
            Name = "DeletedChildGroup",
            Roles = new Role[] { new Role { Name = "noadmin" } },
            Children = new List<Group>()
            {
                DeletedGroup
            }
        };

        public static Group ParentGroupWithDeletedAndNotDeleted => new Group()
        {
            Name = "DeletedChildGroup",
            Roles = new Role[] { new Role { Name = "noadmin" } },
            Children = new List<Group>()
            {
                DeletedGroup,
                JobAdminGroup
            }
        };

        public static IEnumerable<object[]> SingleUsers => new[] {
            new object[] // if user is not an admin, it should not be changed
            {
                new User("testSubjectId1", "windows")
                {
                    Roles = new Role[] { new Role{ Name = "notadmin" } }
                },
                0,
                1
            },
            new object[] // if user is dos admin, it should be removed from edwadmin
            {
                new User("testSubjectId2", "windows")
                {
                    Roles = new Role[] { new Role{ Name = "dosadmin" } }
                },
                0,
                1
            },
            new object[] // if user is datamart admin, it should be added to edwadmin
            {
                new User("testSubjectId3", "windows")
                {
                    Roles = new Role[] { new Role{ Name = "datamartadmin" } }
                },
                1,
                0
            },
            new object[]  // if user is job admin, it should be added to edwadmin
            {
                new User("testSubjectId4", "windows")
                {
                    Roles = new Role[] { new Role{ Name = "jobadmin" } }
                },
                1,
                0
            },
            new object[]  // if user is deleted, it should be removed from edwadmin
            {
                new User("testSubjectId5", "windows")
                {
                    Roles = new Role[] { new Role{ Name = "jobadmin" } },
                    IsDeleted = true
                },
                0,
                1
            },
        };

        public static IEnumerable<object[]> MultipleUsers => new[]
        {
            new object[] // if one user has admin and one doesnt, then should be a remove and add
            {
                new List<User>() {
                    new User("testSubjectId6", "windows")
                    {
                        Roles = new Role[] { new Role { Name = "notadmin" } }
                    },
                    new User("testSubjectId7", "windows")
                    {
                        Roles = new Role[] { new Role { Name = "datamartadmin" } }
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
                        Roles = new Role[] { new Role { Name = "notadmin" } },
                        Groups = new List<Group>() { DeletedGroup }
                    },
                    new User("testSubjectId9", "windows")
                    {
                        Roles = new Role[] { new Role { Name = "notadmin" } },
                        Groups = new List<Group>() { DeletedGroup }
                    }
                },
                0,
                2
            },
            new object[] // If a group is deleted, then remove all non admins
            {
                new List<User>() {
                    new User("testSubjectId10", "windows")
                    { // gets removed from the group
                        Roles = new Role[] { new Role { Name = "notadmin" } },
                        Groups = new List<Group>() { DeletedGroup }
                    },
                    new User("testSubjectId11", "windows")
                    { // stays in the group
                        Roles = new Role[] { new Role { Name = "datamartadmin" } },
                        Groups = new List<Group>() { DeletedGroup }
                    }
                },
                1,
                1
            },
            new object[] // If a child group has admin role, then add edwadmin
            {
                new List<User>() {
                    new User("testSubjectId12", "windows")
                    { // gets added to admin group
                        Roles = new Role[] { new Role { Name = "notadmin" } },
                        Groups = new List<Group>() { ParentGroup }
                    },
                    new User("testSubjectId13", "windows")
                    { // gets added to admin group
                        Roles = new Role[] { new Role { Name = "notadmin" } },
                        Groups = new List<Group>() { ParentGroup }
                    }
                },
                2,
                0
            },
            new object[] // If a child group has admin role, then add edwadmin
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
                2,
                0
            },
            new object[] // If a child group is deleted, then remove edwadmin
            {
                new List<User>() {
                    new User("testSubjectId14", "windows")
                    { // gets removed from the group
                        Roles = new Role[] { new Role { Name = "notadmin" } },
                        Groups = new List<Group>() { DeletedChildGroup }
                    },
                    new User("testSubjectId15", "windows")
                    { // gets removed from the group
                        Roles = new Role[] { new Role { Name = "notadmin" } },
                        Groups = new List<Group>() { DeletedChildGroup }
                    }
                },
                0,
                2
            },
            new object[] // If a child group has admin role but users already have edwadmin, then add edwadmin
            {
                new List<User>() {
                    new User("testSubjectId16", "windows")
                    { // gets removed from the group
                        Roles = new Role[] { new Role { Name = "datamartadmin" } },
                        Groups = new List<Group>() { DeletedChildGroup }
                    },
                    new User("testSubjectId17", "windows")
                    { // stays in the group
                        Roles = new Role[] { new Role { Name = "datamartadmin" } },
                        Groups = new List<Group>() { DeletedChildGroup }
                    }
                },
                2,
                0
            },
            new object[] // If a child group has admin role but users already have edwadmin, then add edwadmin
            {
                new List<User>() {
                    new User("testSubjectId18", "windows")
                    { // gets removed from the group
                        Roles = new Role[] { new Role { Name = "datamartadmin" } },
                        Groups = new List<Group>() { ParentGroupWithDeletedAndNotDeleted }
                    },
                    new User("testSubjectId19", "windows")
                    { // stays in the group
                        Roles = new Role[] { new Role { Name = "datamartadmin" } },
                        Groups = new List<Group>() { ParentGroupWithDeletedAndNotDeleted }
                    }
                },
                2,
                0
            },
        };
    }
}
