using System;

namespace Fabric.Authorization.API.Models.External.Identity
{
    public class IdentityUserSearchResponse
    {
        public string SubjectId { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
    }
}