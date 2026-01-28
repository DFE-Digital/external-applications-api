using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Serilog.Events;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;

namespace DfE.ExternalApplications.Api.Telemetry;

/// <summary>
/// Custom telemetry converter that creates ExceptionTelemetry for log events
/// that include exceptions, ensuring they appear in App Insights Exceptions table.
/// 
/// For log events without exceptions, it creates TraceTelemetry as usual.
/// </summary>
public class ExceptionTrackingTelemetryConverter : TraceTelemetryConverter
{
    public override IEnumerable<ITelemetry> Convert(LogEvent logEvent, IFormatProvider formatProvider)
    {
        // If the log event has an exception, create ExceptionTelemetry
        if (logEvent.Exception != null)
        {
            var exceptionTelemetry = new ExceptionTelemetry(logEvent.Exception)
            {
                SeverityLevel = ConvertSeverityLevel(logEvent.Level),
                Timestamp = logEvent.Timestamp
            };

            // Add the log message as a property
            exceptionTelemetry.Properties["LogMessage"] = logEvent.RenderMessage(formatProvider);
            
            // Add all log event properties
            foreach (var property in logEvent.Properties)
            {
                var value = property.Value?.ToString();
                if (!string.IsNullOrEmpty(value))
                {
                    // Remove quotes from string values
                    if (value.StartsWith("\"") && value.EndsWith("\""))
                    {
                        value = value[1..^1];
                    }
                    exceptionTelemetry.Properties[property.Key] = value;
                }
            }

            yield return exceptionTelemetry;

            // Also yield the trace so you have both the trace log AND the exception
            // Comment out the next line if you only want ExceptionTelemetry
            foreach (var trace in base.Convert(logEvent, formatProvider))
            {
                yield return trace;
            }
        }
        else
        {
            // No exception - just create trace telemetry as usual
            foreach (var telemetry in base.Convert(logEvent, formatProvider))
            {
                yield return telemetry;
            }
        }
    }

    private static SeverityLevel ConvertSeverityLevel(LogEventLevel level)
    {
        return level switch
        {
            LogEventLevel.Verbose => SeverityLevel.Verbose,
            LogEventLevel.Debug => SeverityLevel.Verbose,
            LogEventLevel.Information => SeverityLevel.Information,
            LogEventLevel.Warning => SeverityLevel.Warning,
            LogEventLevel.Error => SeverityLevel.Error,
            LogEventLevel.Fatal => SeverityLevel.Critical,
            _ => SeverityLevel.Information
        };
    }
}
