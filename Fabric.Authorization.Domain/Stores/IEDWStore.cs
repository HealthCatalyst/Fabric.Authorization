namespace Fabric.Authorization.Domain.Stores
{
    public interface IEDWStore
    {
        void AddIdentitiesToRole(string[] identities, string roleName);
        void RemoveIdentitiesFromRole(string[] identities, string roleName);
    }
}