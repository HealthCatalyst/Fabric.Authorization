using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores
{
    public interface IUserStore : IGenericStore<string, User>
    {
    }
}