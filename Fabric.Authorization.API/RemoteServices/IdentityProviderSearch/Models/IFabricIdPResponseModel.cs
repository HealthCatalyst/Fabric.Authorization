using System.Net;

namespace Fabric.Authorization.API.RemoteServices.IdentityProviderSearch.Models
{
    public interface IFabricIdPResponseModel<T>
    {
        HttpStatusCode HttpStatusCode { get; set; }
        T Result { get; set; }
    }
}
