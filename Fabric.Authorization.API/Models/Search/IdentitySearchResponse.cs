using System;
using System.Collections.Generic;
using Fabric.Authorization.Domain.Models;
using Nancy;
using Newtonsoft.Json;

namespace Fabric.Authorization.API.Models.Search
{
    public enum IdentitySearchResponseEntityType
    {
        User,
        Group
    }

    public class IdentitySearchResponse
    {
        public string SubjectId { get; set; }
        public string IdentityProvider { get; set; }
        public IEnumerable<string> Roles { get; set; } = new List<string>();
        public string GroupName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public DateTime? LastLoginDateTimeUtc { get; set; }
        public string EntityType { get; set; }

        [JsonIgnore]
        public string Name => string.IsNullOrWhiteSpace(GroupName) ? $"{FirstName} {MiddleName} {LastName}".Trim() : GroupName?.Trim();

        public override string ToString()
        {
            return $"SubjectId={SubjectId}, IdentityProvider={IdentityProvider}, Roles={Roles.ToString(Environment.NewLine)}, GroupName={GroupName}, FirstName={FirstName}, MiddleName={MiddleName}, LastName={LastName}, LastLoginDateTimeUtc={LastLoginDateTimeUtc}";
        }
    }

    public class FabricAuthUserSearchResponse
    {
        public HttpStatusCode HttpStatusCode { get; set; }
        public IEnumerable<IdentitySearchResponse> Results { get; set; }
    }
}