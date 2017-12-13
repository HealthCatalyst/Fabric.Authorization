using System;
using System.Threading.Tasks;
using Serilog;

namespace Fabric.Authorization.Domain.Events
{
    public class SerilogEventWriter : IEventWriter
    {
        private readonly ILogger _logger;

        public SerilogEventWriter(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task WriteEvent(Event evt)
        {
            _logger.Information("{Id} - {Name}, Details: {@details}", evt.Identifier, evt.Name, evt);
            return Task.CompletedTask;
        }
    }
}
