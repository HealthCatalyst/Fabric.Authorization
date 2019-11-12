using System.Collections.Generic;
using System.Net;

namespace Fabric.Authorization.API.RemoteServices.Identity.Models
{
    public class IdentityGroup
    {
        public string ExternalIdentifier { get; set; }
        public string GroupName { get; set; }
        public string TenantId { get; set; }
        public string IdentityProvider { get; set; }
        public string PrincipalType { get; set; }
    }

    public class GroupSearchResponse
    {
        public IEnumerable<IdentityGroup> Principals { get; set; }
        public int ResultCount { get; set; }
    }

    public class FabricIdentityGroupResponse
    {
        public HttpStatusCode HttpStatusCode { get; set; }
        public IEnumerable<IdentityGroup> Results { get; set; }
    }
}
