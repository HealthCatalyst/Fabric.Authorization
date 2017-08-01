using System;
using Fabric.Authorization.API.Constants;
using Fabric.Authorization.API.Infrastructure;
using Fabric.Authorization.Domain.Services;
using IdentityModel;
using Nancy;

namespace Fabric.Authorization.API.Services
{
    public class EventContextResolverService : IEventContextResolverService
    {
        private readonly NancyContext _context;
        public EventContextResolverService(NancyContextWrapper contextWrapper)
        {
            _context = contextWrapper.Context;
        }

        public string Username => _context?.CurrentUser?.Identity.Name;
        public string ClientId => _context?.CurrentUser?.FindFirst(Claims.ClientId)?.Value;
        public string Subject => _context?.CurrentUser?.FindFirst(JwtClaimTypes.Subject)?.Value;
        public string RemoteIpAddress => _context?.Request?.UserHostAddress;
    }
}
