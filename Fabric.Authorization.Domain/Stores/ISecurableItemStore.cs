using System.Threading.Tasks;
using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores
{
    public interface ISecurableItemStore
    {
        Task<SecurableItem> Get(string name);
    }
}
