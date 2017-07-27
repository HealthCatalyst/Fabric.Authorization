using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.API.Configuration;
using Serilog;
using Serilog.Core;

namespace Fabric.Authorization.API.Logging
{
    public class LogFactory
    {
        public static ILogger CreateTraceLogger(LoggingLevelSwitch levelSwitch, ApplicationInsights appInsightsConfig)
        {
            var loggerConfiguration = CreateLoggerConfiguration(levelSwitch);

            if (appInsightsConfig != null && appInsightsConfig.Enabled &&
                !string.IsNullOrEmpty(appInsightsConfig.InstrumentationKey))
            {
                loggerConfiguration.WriteTo.ApplicationInsightsTraces(appInsightsConfig.InstrumentationKey);
            }

            return loggerConfiguration.CreateLogger();
        }

        public static ILogger CreateEventLogger(LoggingLevelSwitch levelSwitch, ApplicationInsights appInsightsConfig)
        {
            var loggerConfiguration = CreateLoggerConfiguration(levelSwitch);

            if (appInsightsConfig != null && appInsightsConfig.Enabled &&
                !string.IsNullOrEmpty(appInsightsConfig.InstrumentationKey))
            {
                loggerConfiguration.WriteTo.ApplicationInsightsEvents(appInsightsConfig.InstrumentationKey);
            }

            return loggerConfiguration.CreateLogger();
        }

        private static LoggerConfiguration CreateLoggerConfiguration(LoggingLevelSwitch levelSwitch)
        {
            return new LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch)
                .Enrich.FromLogContext()
                .WriteTo.ColoredConsole();
        }
    }
}
