using System.Collections.Concurrent;
using DfE.ExternalApplications.Domain.Tenancy;
using GovUK.Dfe.CoreLibs.Messaging.Contracts.Entities.Topics;
using GovUK.Dfe.CoreLibs.Messaging.Contracts.Messages.Events;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DfE.ExternalApplications.Application.Services;

/// <summary>
/// Factory that creates and caches per-tenant MassTransit bus instances.
/// Each tenant gets its own Azure Service Bus connection based on their configuration.
/// </summary>
public class TenantBusFactory : ITenantBusFactory
{
    private readonly ConcurrentDictionary<Guid, IBusControl> _buses = new();
    private readonly ITenantConfigurationProvider _tenantProvider;
    private readonly ILogger<TenantBusFactory> _logger;
    private bool _disposed;

    public TenantBusFactory(
        ITenantConfigurationProvider tenantProvider,
        ILogger<TenantBusFactory> logger)
    {
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public IBus GetBus(Guid tenantId)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(TenantBusFactory));
        }

        return _buses.GetOrAdd(tenantId, CreateBus);
    }

    public bool HasBus(Guid tenantId)
    {
        return _buses.ContainsKey(tenantId);
    }

    private IBusControl CreateBus(Guid tenantId)
    {
        var tenant = _tenantProvider.GetTenant(tenantId);
        if (tenant == null)
        {
            throw new InvalidOperationException($"Tenant '{tenantId}' not found.");
        }

        var connectionString = tenant.GetConnectionString("ServiceBus");
        if (string.IsNullOrEmpty(connectionString))
        {
            _logger.LogWarning(
                "Tenant {TenantId} ({TenantName}) has no ServiceBus connection string. Using in-memory bus.",
                tenantId, tenant.Name);
            
            // Create in-memory bus for development/testing
            var inMemoryBus = Bus.Factory.CreateUsingInMemory(cfg =>
            {
                cfg.Message<ScanRequestedEvent>(m => m.SetEntityName(TopicNames.ScanRequests));
                cfg.Message<ScanResultEvent>(m => m.SetEntityName(TopicNames.ScanResult));
            });
            
            inMemoryBus.Start();
            return inMemoryBus;
        }

        _logger.LogInformation(
            "Creating Azure Service Bus for tenant {TenantId} ({TenantName})",
            tenantId, tenant.Name);

        var bus = Bus.Factory.CreateUsingAzureServiceBus(cfg =>
        {
            cfg.Host(connectionString);
            
            // Configure message types for topic routing
            cfg.Message<ScanRequestedEvent>(m => m.SetEntityName(TopicNames.ScanRequests));
            cfg.Message<ScanResultEvent>(m => m.SetEntityName(TopicNames.ScanResult));
            
            cfg.UseJsonSerializer();
        });

        bus.Start();
        
        _logger.LogInformation(
            "Started Azure Service Bus for tenant {TenantId} ({TenantName})",
            tenantId, tenant.Name);

        return bus;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var (tenantId, bus) in _buses)
        {
            try
            {
                _logger.LogInformation("Stopping bus for tenant {TenantId}", tenantId);
                await bus.StopAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping bus for tenant {TenantId}", tenantId);
            }
        }

        _buses.Clear();
    }
}

