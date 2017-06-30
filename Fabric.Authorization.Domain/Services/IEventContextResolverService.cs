namespace Fabric.Authorization.Domain.Services
{
    public interface IEventContextResolverService
    {
        string Username { get; }
        string ClientId { get; }
        string Subject { get; }
        string RemoteIpAddress { get; }
    }
}
