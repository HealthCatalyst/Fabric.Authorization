using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using Fabric.Authorization.Domain.Services;

namespace Fabric.Authorization.UnitTests.Mocks
{
    public static class EventContextResolverMockExtensions
    {
        public static Mock<IEventContextResolverService> Setup(this Mock<IEventContextResolverService> mockContextResolverService,
            string username, string clientId, string subject, string remoteIpAddress)
        {
            mockContextResolverService.Setup(contextResolverService => contextResolverService.ClientId)
                .Returns("fabric-authorization");
            mockContextResolverService.Setup(contextResolverService => contextResolverService.Subject)
                .Returns("123456");
            mockContextResolverService.Setup(contextResolverService => contextResolverService.Username)
                .Returns("bob");
            mockContextResolverService.Setup(contextResolverService => contextResolverService.RemoteIpAddress)
                .Returns("192.168.0.1");
            return mockContextResolverService;
        }
    }
}
