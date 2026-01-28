using System.Text.RegularExpressions;
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
/// 
/// This converter also extracts ErrorId and CorrelationId from log messages
/// to make them searchable in App Insights.
/// </summary>
public class ExceptionTrackingTelemetryConverter : TraceTelemetryConverter
{
    // Regex patterns to extract ErrorId and CorrelationId from log messages
    private static readonly Regex ErrorIdPattern = new(@"ErrorId[:\s]+(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex CorrelationIdPattern = new(@"CorrelationId[:\s]+([a-f0-9\-]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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

            // Add the log message as a property (this makes the full message searchable)
            var renderedMessage = logEvent.RenderMessage(formatProvider);
            exceptionTelemetry.Properties["LogMessage"] = renderedMessage;
            
            // Extract ErrorId from message if present (for searchability)
            var errorIdMatch = ErrorIdPattern.Match(renderedMessage);
            if (errorIdMatch.Success)
            {
                exceptionTelemetry.Properties["ErrorId"] = errorIdMatch.Groups[1].Value;
            }
            
            // Extract CorrelationId from message if present
            var correlationIdMatch = CorrelationIdPattern.Match(renderedMessage);
            if (correlationIdMatch.Success)
            {
                exceptionTelemetry.Properties["CorrelationId"] = correlationIdMatch.Groups[1].Value;
            }
            
            // Add all log event properties (structured logging properties)
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
                    
                    // Don't overwrite if already set from message parsing
                    if (!exceptionTelemetry.Properties.ContainsKey(property.Key))
                    {
                        exceptionTelemetry.Properties[property.Key] = value;
                    }
                }
            }

            yield return exceptionTelemetry;

            // Also yield the trace so you have both the trace log AND the exception
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
