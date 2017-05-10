namespace Fabric.Authorization.Domain.Clients
{
    public interface IClientStore
    {
        Client GetClient(string clientId);
        Client Add(Client client);
    }
}
