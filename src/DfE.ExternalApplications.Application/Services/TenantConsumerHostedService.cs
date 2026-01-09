using DfE.ExternalApplications.Application.Consumers;
using DfE.ExternalApplications.Domain.Tenancy;
using GovUK.Dfe.CoreLibs.Messaging.Contracts.Entities.Topics;
using GovUK.Dfe.CoreLibs.Messaging.Contracts.Exceptions;
using GovUK.Dfe.CoreLibs.Messaging.Contracts.Messages.Events;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DfE.ExternalApplications.Application.Services;

/// <summary>
/// Hosted service that starts consumer buses for all configured tenants.
/// Each tenant gets its own subscription endpoint based on their MassTransit configuration.
/// </summary>
public class TenantConsumerHostedService : IHostedService, IAsyncDisposable
{
    private readonly List<IBusControl> _consumerBuses = new();
    private readonly ITenantConfigurationProvider _tenantProvider;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TenantConsumerHostedService> _logger;

    public TenantConsumerHostedService(
        ITenantConfigurationProvider tenantProvider,
        IServiceProvider serviceProvider,
        ILogger<TenantConsumerHostedService> logger)
    {
        _tenantProvider = tenantProvider;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var tenant in _tenantProvider.GetAllTenants())
        {
            try
            {
                var bus = await CreateAndStartConsumerBus(tenant, cancellationToken);
                if (bus != null)
                {
                    _consumerBuses.Add(bus);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Failed to start consumer bus for tenant {TenantId} ({TenantName})", 
                    tenant.Id, tenant.Name);
            }
        }

        _logger.LogInformation(
            "Started {Count} tenant consumer bus(es)", 
            _consumerBuses.Count);
    }

    private async Task<IBusControl?> CreateAndStartConsumerBus(
        TenantConfiguration tenant, 
        CancellationToken cancellationToken)
    {
        var connectionString = tenant.GetConnectionString("ServiceBus");
        if (string.IsNullOrEmpty(connectionString))
        {
            _logger.LogInformation(
                "Tenant {TenantId} ({TenantName}) has no ServiceBus connection - skipping consumer",
                tenant.Id, tenant.Name);
            return null;
        }

        // Skip dummy/placeholder connection strings that would cause hanging
        if (IsDummyConnectionString(connectionString))
        {
            _logger.LogInformation(
                "Tenant {TenantId} ({TenantName}) has a placeholder ServiceBus connection - skipping consumer",
                tenant.Id, tenant.Name);
            return null;
        }

        // Read subscription name from tenant's MassTransit config
        var subscriptionName = tenant.Settings["MassTransit:AzureServiceBus:SubscriptionName"] ?? "extapi";

        _logger.LogInformation(
            "Starting consumer bus for tenant {TenantId} ({TenantName}) with subscription '{Subscription}'",
            tenant.Id, tenant.Name, subscriptionName);

        var bus = Bus.Factory.CreateUsingAzureServiceBus(cfg =>
        {
            cfg.Host(connectionString);
            
            // Configure message types for topic routing
            cfg.Message<ScanResultEvent>(m => m.SetEntityName(TopicNames.ScanResult));
            
            cfg.UseJsonSerializer();

            // Configure subscription endpoint with tenant-specific name
            cfg.SubscriptionEndpoint<ScanResultEvent>(subscriptionName, e =>
            {
                e.UseMessageRetry(r =>
                {
                    // For MessageNotForThisInstanceException (instance filtering in Local env)
                    r.Handle<MessageNotForThisInstanceException>();
                    r.Immediate(10);

                    // For all OTHER exceptions (real errors)
                    r.Ignore<MessageNotForThisInstanceException>();
                    r.Interval(3, TimeSpan.FromSeconds(5));
                });

                // Don't try to create new topology - use existing subscription
                e.ConfigureConsumeTopology = false;

                // Configure the consumer with scoped DI resolution
                e.Consumer(() => CreateScopedConsumer(tenant.Id));
            });
        });

        // Use a timeout to prevent hanging on invalid connections
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));

        try
        {
            await bus.StartAsync(timeoutCts.Token);

            _logger.LogInformation(
                "Started consumer bus for tenant {TenantId} ({TenantName}) with subscription '{Subscription}'",
                tenant.Id, tenant.Name, subscriptionName);

            return bus;
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Timeout starting consumer bus for tenant {TenantId} ({TenantName}) - connection may be invalid",
                tenant.Id, tenant.Name);
            
            // Try to stop the bus gracefully
            try { await bus.StopAsync(CancellationToken.None); } catch { /* ignore */ }
            
            return null;
        }
    }

    /// <summary>
    /// Checks if the connection string is a dummy/placeholder that would cause connection to hang.
    /// </summary>
    private static bool IsDummyConnectionString(string connectionString)
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
    /// Creates a consumer instance using a new DI scope.
    /// Sets the tenant context so the consumer can access tenant-specific services.
    /// </summary>
    private ScanResultConsumer CreateScopedConsumer(Guid tenantId)
    {
        var scope = _serviceProvider.CreateScope();
        
        // Set tenant context for this scope
        var tenantAccessor = scope.ServiceProvider.GetService<ITenantContextAccessor>();
        if (tenantAccessor != null)
        {
            var tenant = _tenantProvider.GetTenant(tenantId);
            if (tenant != null)
            {
                tenantAccessor.CurrentTenant = tenant;
            }
        }
        
        return scope.ServiceProvider.GetRequiredService<ScanResultConsumer>();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping {Count} tenant consumer bus(es)", _consumerBuses.Count);

        foreach (var bus in _consumerBuses)
        {
            try
            {
                await bus.StopAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping consumer bus");
            }
        }

        _consumerBuses.Clear();
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None);
    }
}
