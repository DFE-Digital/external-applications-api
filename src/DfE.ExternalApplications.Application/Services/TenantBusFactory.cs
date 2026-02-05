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
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _busLocks = new();
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

    public async Task<IBus> GetBusAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(TenantBusFactory));
        }

        // Check if bus already exists and is started
        if (_buses.TryGetValue(tenantId, out var existingBus))
        {
            return existingBus;
        }

        // Use per-tenant lock to prevent concurrent bus creation for same tenant
        var busLock = _busLocks.GetOrAdd(tenantId, _ => new SemaphoreSlim(1, 1));
        await busLock.WaitAsync(cancellationToken);
        
        try
        {
            // Double-check after acquiring lock
            if (_buses.TryGetValue(tenantId, out existingBus))
            {
                return existingBus;
            }

            var bus = await CreateAndStartBusAsync(tenantId, cancellationToken);
            _buses[tenantId] = bus;
            return bus;
        }
        finally
        {
            busLock.Release();
        }
    }

    public bool HasBus(Guid tenantId)
    {
        return _buses.ContainsKey(tenantId);
    }

    private async Task<IBusControl> CreateAndStartBusAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenant = _tenantProvider.GetTenant(tenantId);
        if (tenant == null)
        {
            throw new InvalidOperationException($"Tenant '{tenantId}' not found.");
        }

        var connectionString = tenant.GetConnectionString("ServiceBus");
        
        // Log the connection string being used (masked for security)
        var maskedConnectionString = MaskConnectionString(connectionString);
        _logger.LogInformation(
            "Tenant {TenantId} ({TenantName}) ServiceBus connection for publishing: {ConnectionString}",
            tenantId, tenant.Name, maskedConnectionString);

        if (string.IsNullOrEmpty(connectionString) || IsDummyConnectionString(connectionString))
        {
            _logger.LogWarning(
                "Tenant {TenantId} ({TenantName}) has no valid ServiceBus connection string. Using in-memory bus.",
                tenantId, tenant.Name);
            
            // Create in-memory bus for development/testing
            var inMemoryBus = Bus.Factory.CreateUsingInMemory(cfg =>
            {
                cfg.Message<ScanRequestedEvent>(m => m.SetEntityName(TopicNames.ScanRequests));
                cfg.Message<ScanResultEvent>(m => m.SetEntityName(TopicNames.ScanResult));
            });
            
            await inMemoryBus.StartAsync(cancellationToken);
            _logger.LogInformation(
                "Started in-memory bus for tenant {TenantId} ({TenantName})",
                tenantId, tenant.Name);
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

        // Start the bus asynchronously and wait for it to be ready
        _logger.LogInformation(
            "Starting Azure Service Bus for tenant {TenantId} ({TenantName})...",
            tenantId, tenant.Name);

        // Use a timeout to prevent indefinite waiting
        using var startupCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        startupCts.CancelAfter(TimeSpan.FromSeconds(30));
        
        try
        {
            await bus.StartAsync(startupCts.Token);
            
            _logger.LogInformation(
                "Started Azure Service Bus for tenant {TenantId} ({TenantName})",
                tenantId, tenant.Name);

            return bus;
        }
        catch (OperationCanceledException) when (startupCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(
                "Timeout starting Azure Service Bus for tenant {TenantId} ({TenantName})",
                tenantId, tenant.Name);
            throw new TimeoutException($"Timeout starting Azure Service Bus for tenant '{tenant.Name}'");
        }
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

    /// <summary>
    /// Checks if the connection string is a dummy/placeholder that would cause connection to hang.
    /// </summary>
    private static bool IsDummyConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return true;

        // Common patterns for dummy/placeholder connection strings
        var dummyPatterns = new[]
        {
            "dummy",
            "placeholder",
            "localhost",
            "example.com",
            "your-namespace",
            "SharedAccessKey=dummy",
            "SharedAccessKey=secret",
            "SharedAccessKey=your"
        };

        return dummyPatterns.Any(pattern => 
            connectionString.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Masks sensitive parts of the connection string for logging.
    /// </summary>
    private static string MaskConnectionString(string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return "(empty)";

        // Extract the namespace for identification
        var endpointMatch = System.Text.RegularExpressions.Regex.Match(
            connectionString, 
            @"Endpoint=sb://([^/;]+)", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (endpointMatch.Success)
        {
            return $"sb://{endpointMatch.Groups[1].Value}/...";
        }

        // If can't parse, just show first 20 chars
        return connectionString.Length > 20 
            ? connectionString[..20] + "..." 
            : connectionString;
    }
}

