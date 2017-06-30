using System;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.Domain.Services;
using IdentityModel;
using Nancy;

namespace Fabric.Authorization.API.Services
{
    public class EventContextResolverService : IEventContextResolverService
    {
        private readonly NancyContext _context;
        public EventContextResolverService(NancyContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public string Username => _context.CurrentUser?.Identity.Name;
        public string ClientId => _context.CurrentUser?.FindFirst(Claims.ClientId)?.Value;
        public string Subject => _context.CurrentUser?.FindFirst(JwtClaimTypes.Subject)?.Value;
        public string RemoteIpAddress => _context.Request?.UserHostAddress;
    }
}
