using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace Fabric.Authorization.UnitTests.Caching
{
    public class CachingDocumentDbServiceTests
    {
        [Fact]
        public void GetDocument_CachesDocument()
        {
            //Arrange
            var permissions = SetupPermissions();
            var permission = permissions.Values.First();
            var mockDbAccessService = SetupMockDocumentDbService(permissions);
            var cachingDocumentDbService = new CachingDocumentDbService(mockDbAccessService.Object,
                new MemoryCache(new MemoryCacheOptions()));
            //Act
            AssertPermissionRetrievedAsync(permission, cachingDocumentDbService).Wait();
            AssertPermissionRetrievedAsync(permission, cachingDocumentDbService).Wait();

            //Assert
            mockDbAccessService.Verify(dbAccessService => dbAccessService.GetDocument<Permission>(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void UpdateDocument_InvalidatesCache()
        {
            //Arrange
            var permissions = SetupPermissions();
            var permission = permissions.Values.First();
            var mockDbAccessService = SetupMockDocumentDbService(permissions);
            var cachingDocumentDbService = new CachingDocumentDbService(mockDbAccessService.Object,
                new MemoryCache(new MemoryCacheOptions()));

            //Act
            AssertPermissionRetrievedAsync(permission, cachingDocumentDbService).Wait();
            cachingDocumentDbService.UpdateDocument(permission.Identifier, permission).Wait();
            AssertPermissionRetrievedAsync(permission, cachingDocumentDbService).Wait();

            //Assert
            mockDbAccessService.Verify(dbAccessService => dbAccessService.GetDocument<Permission>(It.IsAny<string>()), Times.Exactly(2));
        }

        [Fact]
        public void BulkUpdate_UpdateDocument_InvalidatesCache()
        {
            var permissions = SetupPermissions();
            var permissionObjects = permissions.Values.ToList();
            var permission1 = permissionObjects[0];
            var permission2 = permissionObjects[1];

            var mockDbAccessService = SetupMockDocumentDbService(permissions);
            var cachingDocumentDbService = new CachingDocumentDbService(mockDbAccessService.Object,
                new MemoryCache(new MemoryCacheOptions()));

            AssertPermissionRetrievedAsync(permission1, cachingDocumentDbService).Wait();
            AssertPermissionRetrievedAsync(permission2, cachingDocumentDbService).Wait();

            cachingDocumentDbService.BulkUpdateDocuments(new List<string> {permission1.Id.ToString(), permission2.Id.ToString()},
                permissionObjects).Wait();

            mockDbAccessService.Verify(dbAccessService => dbAccessService.GetDocument<Permission>(It.IsAny<string>()), Times.Exactly(2));
        }

        [Fact]
        public void DeleteDocument_InvalidatesCache()
        {
            //Arrange
            var permissions = SetupPermissions();
            var permission = permissions.Values.First();
            var mockDbAccessService = SetupMockDocumentDbService(permissions);
            var cachingDocumentDbService = new CachingDocumentDbService(mockDbAccessService.Object,
                new MemoryCache(new MemoryCacheOptions()));
            //Act
            AssertPermissionRetrievedAsync(permission, cachingDocumentDbService).Wait();
            cachingDocumentDbService.DeleteDocument<Permission>(permission.Identifier).Wait();
            AssertPermissionRetrievedAsync(permission, cachingDocumentDbService).Wait();

            //Assert
            mockDbAccessService.Verify(dbAccessService => dbAccessService.GetDocument<Permission>(It.IsAny<string>()), Times.Exactly(2));
        }

        private static Dictionary<string, Permission> SetupPermissions()
        {
            var permissions = new Dictionary<string, Permission>();
            var permission = new Permission
            {
                Name = "test",
                Grain = "app",
                SecurableItem = "test",
                Id = Guid.NewGuid()
            };
            permissions.Add(permission.Identifier, permission);

            permission = new Permission
            {
                Name = "test",
                Grain = "app",
                SecurableItem = "test2",
                Id = Guid.NewGuid()
            };
            permissions.Add(permission.Identifier, permission);
            return permissions;
        }

        private static Mock<IDocumentDbService> SetupMockDocumentDbService(IReadOnlyDictionary<string, Permission> permissions)
        {
            var mockDbAccessService = new Mock<IDocumentDbService>();
            mockDbAccessService.Setup(dbAccessService => dbAccessService.GetDocument<Permission>(It.IsAny<string>()))
                .ReturnsAsync((string documentId) => permissions[documentId]);
            return mockDbAccessService;
        }

        private static async Task AssertPermissionRetrievedAsync(Permission permission, IDocumentDbService cachingDocumentDbService)
        {
            var retrievedPermission = await cachingDocumentDbService.GetDocument<Permission>(permission.Identifier);
            Assert.Equal(permission.Id, retrievedPermission.Id);
        }
    }
}