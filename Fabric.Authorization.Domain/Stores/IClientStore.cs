namespace Fabric.Authorization.Domain.Stores
{
    public interface IClientStore
    {
        Client GetClient(string clientId);
        Client Add(Client client);
    }
}
