using DfE.ExternalApplications.Domain.Tenancy;
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

        var bus = _busFactory.GetBus(tenant.Id);

        _logger.LogDebug(
            "Publishing {MessageType} to tenant {TenantId} ({TenantName})",
            typeof(T).Name, tenant.Id, tenant.Name);

        await bus.Publish(message, ctx =>
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
            "Published {MessageType} to tenant {TenantId}",
            typeof(T).Name, tenant.Id);
    }
}
