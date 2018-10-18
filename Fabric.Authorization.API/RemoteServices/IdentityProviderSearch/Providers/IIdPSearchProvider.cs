using System.Threading.Tasks;
using Fabric.Authorization.API.RemoteServices.IdentityProviderSearch.Models;

namespace Fabric.Authorization.API.RemoteServices.IdentityProviderSearch.Providers
{
    public interface IIdPSearchProvider
    {
        Task<IdPPrincipalSearchResponse> Search(IdPPrincipalSearchRequest request);
    }
}
