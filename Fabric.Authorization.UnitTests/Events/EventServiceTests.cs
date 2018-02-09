using System;
using System.Collections.Generic;
using System.Linq;
using Fabric.Authorization.Domain.Events;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.UnitTests.Mocks;
using Moq;
using Xunit;

namespace Fabric.Authorization.UnitTests.Events
{
    public class EventServiceTests
    {
        [Theory, MemberData(nameof(EventServiceData))]
        public void RaiseEvent_Succeeds(string username, string clientId, string subject, string remoteIpAddress)
        {
            var events = new List<Event>();
            var mockEventWriter = new Mock<IEventWriter>();
            mockEventWriter.SetupWriteEvent(events);
            var mockContextResolverService = new Mock<IEventContextResolverService>();
            mockContextResolverService.Setup(username, clientId, subject, remoteIpAddress);

            var eventService = new EventService(mockContextResolverService.Object, mockEventWriter.Object);
            var permission = new Permission
            {
                Id = Guid.NewGuid(),
                Name = "manageusers",
                Grain = "app",
                SecurableItem = "patientsafety"
            };
            eventService.RaiseEventAsync(new EntityAuditEvent<Permission>(EventTypes.EntityCreatedEvent,
                permission.Id.ToString(), permission));

            Assert.Single(events);
            var evt = events.First();
            Assert.Equal(mockContextResolverService.Object.Username, evt.Username);
            Assert.Equal(mockContextResolverService.Object.ClientId, evt.ClientId);
            Assert.Equal(mockContextResolverService.Object.RemoteIpAddress, evt.RemoteIpAddress);
            Assert.Equal(mockContextResolverService.Object.Subject, evt.Subject);
            Assert.NotEqual(default(DateTime), evt.Timestamp);
        }

        public static IEnumerable<object[]> EventServiceData => new[]
        {
            new object[] {"bob", "fabric-authorization", "123456", "192.168.0.1"},
            new object[] {string.Empty, string.Empty, string.Empty, string.Empty},
            new object[] {null, null, null, null}, 
        };
    }
}
