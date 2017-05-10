namespace Fabric.Authorization.Domain.Stores
{
    public interface IGroupStore
    {
        Group GetGroup(string groupName);
    }
}
