using System.Net;

namespace Fabric.Authorization.API.RemoteServices.IdentityProviderSearch.Models
{
    public interface IFabricIdPSearchResponseModel<T>
    {
        HttpStatusCode HttpStatusCode { get; set; }
        T Result { get; set; }
    }
}
