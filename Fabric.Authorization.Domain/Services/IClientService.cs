namespace Fabric.Authorization.Domain.Services
{
    public interface IClientService
    {
        bool DoesClientOwnResource(string clientId, string grain, string resource);
    }
}
