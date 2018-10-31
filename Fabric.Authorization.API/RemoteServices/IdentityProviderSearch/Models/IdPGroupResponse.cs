using System.Collections.Generic;
using System.Net;

namespace Fabric.Authorization.API.RemoteServices.IdentityProviderSearch.Models
{
    public class IdPGroup
    {
        public string GroupId { get; set; }
        public string GroupName { get; set; }
        public string TenantId { get; set; }
        public string IdentityProvider { get; set; }
        public string PrincipalType { get; set; }
    }

    public class IdPGroupResponse
    {
        public IEnumerable<IdPGroup> Principals { get; set; }
        public int ResultCount { get; set; }
    }

    public class FabricIdPGroupResponse
    {
        public HttpStatusCode HttpStatusCode { get; set; }
        public IEnumerable<IdPGroup> Results { get; set; }
    }
}
