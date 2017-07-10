using System;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Events;

namespace Fabric.Authorization.Domain.Services
{
    public class EventService : IEventService
    {
        private readonly IEventContextResolverService _eventContextResolverService;
        private readonly IEventWriter _eventWriter;
        public EventService(IEventContextResolverService eventContextResolverService, IEventWriter eventWriter)
        {
            _eventContextResolverService = eventContextResolverService ??
                                           throw new ArgumentNullException(nameof(eventContextResolverService));
            _eventWriter = eventWriter ?? throw new ArgumentNullException(nameof(eventWriter));
        }

        public Task RaiseEventAsync(Event evnt)
        {
            var eventToWrite = EnrichEventWithContext(evnt);
            _eventWriter.WriteEvent(eventToWrite);
            return Task.CompletedTask;
        }


        private Event EnrichEventWithContext(Event evnt)
        {
            evnt.Timestamp = DateTime.UtcNow;
            evnt.Username = _eventContextResolverService.Username;
            evnt.ClientId = _eventContextResolverService.ClientId;
            evnt.Subject = _eventContextResolverService.Subject;
            evnt.RemoteIpAddress = _eventContextResolverService.RemoteIpAddress;
            return evnt;
        }
    }
}
