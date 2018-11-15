using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.UnitTests.Mocks;
using Moq;
using Xunit;

namespace Fabric.Authorization.UnitTests.Groups
{
    [Collection("Group Service Tests")]
    public class GroupServiceTests
    {
        private readonly GroupServiceFixture _fixture;

        public GroupServiceTests(GroupServiceFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void MigrateDuplicateGroups_NoDuplicates_Success()
        {

        }

        [Fact]
        public void MigrateDuplicateGroups_HasDuplicateNames_Success()
        {

        }

        [Fact]
        public void MigrateDuplicateGroups_HasDuplicateIdentifiers_Success()
        {
            // TODO: write test for this once merged to master with GroupIdentifier logic
        }

        [Fact]
        public void AddPermissionToGroup_Succeeds()
        {
        }

        [Fact]
        public void RemovePermissionFromGroup_Succeeds()
        {
        }

        [Fact]
        public void RemovePermissionFromGroup_ThrowsPermissionNotFoundException()
        {
        }
    }

    public class GroupServiceFixture
    {
        private readonly Mock<IGroupStore> _mockGroupStore = new Mock<IGroupStore>();

        public GroupServiceFixture()
        {
            _mockGroupStore.SetupGetGroups(new List<Group>());
        }
    }
}