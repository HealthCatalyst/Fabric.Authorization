using Serilog.Events;

namespace Fabric.Authorization.Persistence.SqlServer.Configuration
{
    public class EntityFrameworkSettings
    {
        public LogEventLevel MinimumLogLevel { get; set; }
    }
}