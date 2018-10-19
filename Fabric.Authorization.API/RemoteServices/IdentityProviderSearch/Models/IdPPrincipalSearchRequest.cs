using System.Collections;

namespace Fabric.Authorization.API.RemoteServices.IdentityProviderSearch.Models
{
    public class IdPPrincipalSearchRequest
    {
        public string SearchText { get; set; }
        public string Type { get; set; }
    }
}
