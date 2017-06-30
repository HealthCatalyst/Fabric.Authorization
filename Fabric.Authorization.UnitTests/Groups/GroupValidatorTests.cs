using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Domain.Validators;
using Fabric.Authorization.UnitTests.Mocks;
using Moq;
using Xunit;

namespace Fabric.Authorization.UnitTests.Groups
{
    public class GroupValidatorTests
    {
        [Theory, MemberData(nameof(GroupRequestData))]
        public void GroupValidator_ValidateGroup_ReturnsInvalidIfModelNotValid(string grain, string securableItem, string GroupName, int errorCount)
        {
            var existingGroup = new Group
            {
                Id = "app",
            };


//            Assert.False(validationResult.IsValid);
//            Assert.NotNull(validationResult.Errors);
//            Assert.Equal(errorCount, validationResult.Errors.Count);
        }

        [Fact]
        public void GroupValidator_ValidateGroup_ReturnsValid()
        {
//            var mockGroupStore = new Mock<IGroupStore>().SetupGetGroups(new List<Group>()).Create();
//            var GroupValidator = new GroupValidator(mockGroupStore);
   //         var validationResult = GroupValidator.Validate(new Group
     //       {
       //         Name = "admin"
         //   });

           // Assert.True(validationResult.IsValid);
        }

        public static IEnumerable<object[]> GroupRequestData => new[]
        {
            new object[] { "app", "patientsafety", "", 1},
            new object[] {"app", "", "", 2},
            new object[] {"", "", "", 3},
            new object[] {"app", "patientsafety", null, 1},
            new object[] {"app", null, null, 2},
            new object[] {null, null, null, 3},
            new object[] {"app", "patientsafety", "admin", 1}
        };
    }
}
