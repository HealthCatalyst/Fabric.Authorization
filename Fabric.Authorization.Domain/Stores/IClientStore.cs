using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores
{
    public interface IClientStore : IGenericStore<string, Client>
    {
    }
}
