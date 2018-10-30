using System;
using System.Collections.Generic;
using Catalyst.Fabric.Authorization.Models.Enums;

namespace Catalyst.Fabric.Authorization.Models.Search
{
    public enum MemberSearchResponseEntityType
    {
        User,
        DirectoryGroup,
        CustomGroup
    }

    public class MemberSearchResponse
    {
        public string SubjectId { get; set; }
        public string IdentityProvider { get; set; }
        public IEnumerable<RoleApiModel> Roles { get; set; } = new List<RoleApiModel>();
        public string GroupName { get; set; }
        public string DisplayName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public DateTime? LastLoginDateTimeUtc { get; set; }
        public string EntityType { get; set; }
        public string TenantId { get; set; }

        public override string ToString()
        {
            return $"SubjectId={SubjectId}, IdentityProvider={IdentityProvider}, Roles={Roles.ToString(Environment.NewLine)}, GroupName={GroupName}, FirstName={FirstName}, MiddleName={MiddleName}, LastName={LastName}, LastLoginDateTimeUtc={LastLoginDateTimeUtc}";
        }
    }

    public class FabricAuthUserSearchResponse
    {
        public HttpStatusCode HttpStatusCode { get; set; }
        public int TotalCount { get; set; }
        public IEnumerable<MemberSearchResponse> Results { get; set; }
    }

    public class MemberSearchResponseApiModel
    {
        public int TotalCount { get; set; }
        public IEnumerable<MemberSearchResponse> Results { get; set; }
    }
}