using System.Collections.Generic;
using Xunit;

namespace Fabric.Authorization.UnitTests.Groups
{
    public class GroupValidatorTests
    {
        [Theory, MemberData(nameof(GroupRequestData))]
        public void GroupValidator_ValidateGroup_ReturnsInvalidIfModelNotValid(string grain, string securableItem, string GroupName, int errorCount)
        {
            // To be implemented
        }

        [Fact]
        public void GroupValidator_ValidateGroup_ReturnsValid()
        {
            // To be implemented
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