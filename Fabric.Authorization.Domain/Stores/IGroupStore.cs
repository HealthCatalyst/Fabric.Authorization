using Fabric.Authorization.Domain.Models;

namespace Fabric.Authorization.Domain.Stores
{
    public interface IGroupStore
    {
        Group GetGroup(string groupName);

        bool GroupExists(string groupName);
    }
}
