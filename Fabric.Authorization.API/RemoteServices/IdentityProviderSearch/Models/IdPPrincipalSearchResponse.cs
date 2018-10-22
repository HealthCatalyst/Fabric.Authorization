using System.Collections.Generic;
using System.Net;

namespace Fabric.Authorization.API.RemoteServices.IdentityProviderSearch.Models
{
    public class IdPPrincipalSearchResponse
    {
        public ICollection<IdPPrincipal> Principals { get; set; } = new List<IdPPrincipal>();
        public int ResultCount { get; set; }
    }

    public class IdPPrincipal
    {
        public string SubjectId { get; set; }
        public string UserPrincipal { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string PrincipalType { get; set; }
        public string ExternalIdentifier { get; set; }
        public string TenantId { get; set; }
        public string IdentityProvider { get; set; }
    }

    public class FabricIdPSearchResponse : IFabricIdPResponseModel<IdPPrincipalSearchResponse>
    {
        public HttpStatusCode HttpStatusCode { get; set; }
        public IdPPrincipalSearchResponse Result { get; set; }
    }
}
