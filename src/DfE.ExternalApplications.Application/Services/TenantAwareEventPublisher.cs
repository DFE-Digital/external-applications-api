using DfE.ExternalApplications.Domain.Tenancy;
using GovUK.Dfe.CoreLibs.Messaging.Contracts.Entities.Topics;
using GovUK.Dfe.CoreLibs.Messaging.Contracts.Messages.Events;
using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Interfaces;
using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Models;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace DfE.ExternalApplications.Application.Services;

/// <summary>
/// Event publisher that routes messages to the appropriate tenant's Service Bus.
/// Automatically adds tenant ID to message metadata for consumer resolution.
/// </summary>
public class TenantAwareEventPublisher : IEventPublisher
{
    private readonly ITenantContextAccessor _tenantAccessor;
    private readonly ITenantBusFactory _busFactory;
    private readonly ILogger<TenantAwareEventPublisher> _logger;

    // Topic name mapping for message types
    private static readonly Dictionary<Type, string> TopicNames = new()
    {
        { typeof(ScanRequestedEvent), GovUK.Dfe.CoreLibs.Messaging.Contracts.Entities.Topics.TopicNames.ScanRequests },
        { typeof(ScanResultEvent), GovUK.Dfe.CoreLibs.Messaging.Contracts.Entities.Topics.TopicNames.ScanResult }
    };

    public TenantAwareEventPublisher(
        ITenantContextAccessor tenantAccessor,
        ITenantBusFactory busFactory,
        ILogger<TenantAwareEventPublisher> logger)
    {
        _tenantAccessor = tenantAccessor;
        _busFactory = busFactory;
        _logger = logger;
    }

    public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        return PublishAsync(message, null, cancellationToken);
    }

    public async Task PublishAsync<T>(
        T message, 
        AzureServiceBusMessageProperties? properties, 
        CancellationToken cancellationToken = default) where T : class
    {
        var tenant = _tenantAccessor.CurrentTenant
            ?? throw new InvalidOperationException("Tenant context is required to publish events.");

        _logger.LogDebug(
            "Getting bus for tenant {TenantId} ({TenantName}) to publish {MessageType}",
            tenant.Id, tenant.Name, typeof(T).Name);

        // Get the bus asynchronously - this ensures it's started and ready
        var bus = await _busFactory.GetBusAsync(tenant.Id, cancellationToken);

        // Get the topic name for this message type
        var topicName = GetTopicName<T>();
        
        // Build the topic URI for Azure Service Bus
        var connectionString = tenant.GetConnectionString("ServiceBus");
        var topicUri = BuildTopicUri(connectionString, topicName);

        _logger.LogDebug(
            "Publishing {MessageType} to topic {TopicUri} for tenant {TenantId} ({TenantName})",
            typeof(T).Name, topicUri, tenant.Id, tenant.Name);

        // Use explicit send endpoint instead of Publish() for more reliable connection
        var sendEndpoint = await bus.GetSendEndpoint(topicUri);

        await sendEndpoint.Send(message, ctx =>
        {
            // Add tenant ID to message headers for consumer resolution
            ctx.Headers.Set("TenantId", tenant.Id.ToString());
            ctx.Headers.Set("TenantName", tenant.Name);

            // Apply custom properties if provided
            if (properties?.CustomProperties != null)
            {
                foreach (var prop in properties.CustomProperties)
                {
                    ctx.Headers.Set(prop.Key, prop.Value?.ToString());
                }
            }
        }, cancellationToken);

        _logger.LogInformation(
            "Published {MessageType} to topic {TopicName} for tenant {TenantId}",
            typeof(T).Name, topicName, tenant.Id);
    }

    private static string GetTopicName<T>()
    {
        if (TopicNames.TryGetValue(typeof(T), out var topicName))
        {
            return topicName;
        }

        // Fallback: use type name as topic name
        return typeof(T).Name.ToLowerInvariant();
    }

    private static Uri BuildTopicUri(string? connectionString, string topicName)
    {
        // Extract the namespace from the connection string
        // Format: Endpoint=sb://namespace.servicebus.windows.net/;...
        if (string.IsNullOrEmpty(connectionString))
        {
            // For in-memory bus, use loopback
            return new Uri($"loopback://localhost/{topicName}");
        }

        var match = System.Text.RegularExpressions.Regex.Match(
            connectionString,
            @"Endpoint=sb://([^/;]+)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (match.Success)
        {
            var namespaceHost = match.Groups[1].Value;
            // Azure Service Bus topic URI format
            return new Uri($"sb://{namespaceHost}/{topicName}");
        }

        throw new InvalidOperationException($"Could not parse Service Bus namespace from connection string");
    }
}
