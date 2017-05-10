namespace Fabric.Authorization.Domain.Services
{
    public interface IClientService
    {
        bool DoesClientOwnItem(string clientId, string grain, string securableItem);
    }
}
