using System;
using System.Collections.Generic;

namespace Fabric.Authorization.API.Models.Search
{
    public class IdentitySearchResponse
    {
        public string SubjectId { get; set; }
        public IEnumerable<string> Roles { get; set; }
        public string GroupName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string Status { get; set; }
        public DateTime? LastLogin { get; set; }
    }
}