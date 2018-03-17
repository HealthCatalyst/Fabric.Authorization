using System;
using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;
using Nancy;
using Newtonsoft.Json;

namespace Fabric.Authorization.API.Models.Search
{
    public enum MemberSearchResponseEntityType
    {
        User,
        Group
    }

    public class MemberSearchResponse
    {
        public string SubjectId { get; set; }
        public string IdentityProvider { get; set; }
        public IEnumerable<RoleApiModel> Roles { get; set; } = new List<RoleApiModel>();
        public string GroupName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public DateTime? LastLoginDateTimeUtc { get; set; }
        public string EntityType { get; set; }

        [JsonIgnore]
        public string Name => string.IsNullOrWhiteSpace(GroupName) ? GetUserName() : GroupName?.Trim();

        private string GetUserName()
        {
            return string.IsNullOrEmpty(FirstName)
                ? SubjectId
                : $"{FirstName} {MiddleName} {LastName}".Trim();
        }

        public override string ToString()
        {
            return $"SubjectId={SubjectId}, IdentityProvider={IdentityProvider}, Roles={Roles.ToString(Environment.NewLine)}, GroupName={GroupName}, FirstName={FirstName}, MiddleName={MiddleName}, LastName={LastName}, LastLoginDateTimeUtc={LastLoginDateTimeUtc}";
        }
    }

    public class FabricAuthUserSearchResponse
    {
        public HttpStatusCode HttpStatusCode { get; set; }
        public IEnumerable<MemberSearchResponse> Results { get; set; }
    }
}