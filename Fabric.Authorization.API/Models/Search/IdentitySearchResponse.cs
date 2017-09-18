using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Fabric.Authorization.API.Models.Search
{
    public class IdentitySearchResponse
    {
        public string SubjectId { get; set; }
        public IEnumerable<string> Roles { get; set; } = new List<string>();
        public string GroupName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public DateTime? LastLogin { get; set; }

        [JsonIgnore]
        public string Name => string.IsNullOrWhiteSpace(GroupName) ? $"{FirstName} {MiddleName} {LastName}" : GroupName;
    }
}