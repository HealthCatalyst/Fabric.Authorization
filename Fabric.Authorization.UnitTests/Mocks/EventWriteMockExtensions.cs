using System.Collections.Generic;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Events;
using Moq;

namespace Fabric.Authorization.UnitTests.Mocks
{
    public static class EventWriteMockExtensions
    {
        public static Mock<IEventWriter> SetupWriteEvent(this Mock<IEventWriter> mockEventWriter, List<Event> events)
        {
            mockEventWriter.Setup(eventWriter => eventWriter.WriteEvent(It.IsAny<Event>()))
                .Returns((Event evt) =>
                {
                    events.Add(evt);
                    return Task.CompletedTask;
                });
            return mockEventWriter;
        }
    }
}
