using System.Net;

namespace Fabric.Authorization.API.RemoteServices.IdentityProviderSearch.Models
{
    public class IdPGroupResponse
    {
        public string SubjectId { get; set; }
        public string DisplayName { get; set; }
        public string PrincipalType { get; set; }
        public string ExternalIdentifier { get; set; }
        public string TenantId { get; set; }
        public string IdentityProvider { get; set; }
    }

    public class FabricIdPGroupResponse : IFabricIdPResponseModel<IdPGroupResponse>
    {
        public HttpStatusCode HttpStatusCode { get; set; }
        public IdPGroupResponse Result { get; set; }
    }
}
