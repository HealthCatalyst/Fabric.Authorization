using System;
using System.Collections.Generic;
using System.Linq;
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
        public async void GetDocument_CachesDocument()
        {
            //Arrange
            var permissions = SetupPermissions();
            var permission = permissions.Values.First();
            var mockDbAccessService = SetupMockDocumentDbService(permissions);
            var cachingDocumentDbService = new CachingDocumentDbService(mockDbAccessService.Object,
                new MemoryCache(new MemoryCacheOptions()));
            //Act
            AssertPermissionRetrieved(permission, cachingDocumentDbService);
            AssertPermissionRetrieved(permission, cachingDocumentDbService);

            //Assert
            mockDbAccessService.Verify(dbAccessService => dbAccessService.GetDocument<Permission>(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async void UpdateDocument_InvalidatesCache()
        {
            //Arrange
            var permissions = SetupPermissions();
            var permission = permissions.Values.First();
            var mockDbAccessService = SetupMockDocumentDbService(permissions);
            var cachingDocumentDbService = new CachingDocumentDbService(mockDbAccessService.Object,
                new MemoryCache(new MemoryCacheOptions()));
            
            //Act
            AssertPermissionRetrieved(permission, cachingDocumentDbService);
            await cachingDocumentDbService.UpdateDocument(permission.Identifier, permission);
            AssertPermissionRetrieved(permission, cachingDocumentDbService);

            //Assert
            mockDbAccessService.Verify(dbAccessService => dbAccessService.GetDocument<Permission>(It.IsAny<string>()), Times.Exactly(2));
        }

        [Fact]
        public async void DeleteDocument_InvalidatesCache()
        {
            //Arrange
            var permissions = SetupPermissions();
            var permission = permissions.Values.First();
            var mockDbAccessService = SetupMockDocumentDbService(permissions);
            var cachingDocumentDbService = new CachingDocumentDbService(mockDbAccessService.Object,
                new MemoryCache(new MemoryCacheOptions()));
            //Act
            AssertPermissionRetrieved(permission, cachingDocumentDbService);
            await cachingDocumentDbService.DeleteDocument<Permission>(permission.Identifier);
            AssertPermissionRetrieved(permission, cachingDocumentDbService);

            //Assert
            mockDbAccessService.Verify(dbAccessService => dbAccessService.GetDocument<Permission>(It.IsAny<string>()), Times.Exactly(2));
        }

        private Dictionary<string, Permission> SetupPermissions()
        {
            var permissions = new Dictionary<string, Permission>();
            var permission = new Permission()
            {
                Name = "test",
                Grain = "app",
                SecurableItem = "test",
                Id = Guid.NewGuid()
            };
            permissions.Add(permission.Identifier, permission);
            return permissions;
        }

        private Mock<IDocumentDbService> SetupMockDocumentDbService(Dictionary<string, Permission> permissions)
        {
            var mockDbAccessService = new Mock<IDocumentDbService>();
            mockDbAccessService.Setup(dbAccessService => dbAccessService.GetDocument<Permission>(It.IsAny<string>()))
                .ReturnsAsync((string documentId) => permissions[documentId]);
            return mockDbAccessService;
        }

        private async void AssertPermissionRetrieved(Permission permission, CachingDocumentDbService cachingDocumentDbService)
        {
            var retrievedPermission = await cachingDocumentDbService.GetDocument<Permission>(permission.Identifier);
            Assert.Equal(permission.Id, retrievedPermission.Id);
        }
    }
}
