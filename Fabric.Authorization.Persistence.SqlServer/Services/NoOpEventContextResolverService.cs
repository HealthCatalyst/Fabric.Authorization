using Fabric.Authorization.Domain.Services;

namespace Fabric.Authorization.Persistence.SqlServer.Services
{
    public class NoOpEventContextResolverService : IEventContextResolverService
    {
        public string Username { get; set; }
        public string ClientId { get; set; }
        public string Subject { get; set; }
        public string RemoteIpAddress { get; set; }
    }
}
