using MassTransit;

namespace DfE.ExternalApplications.Application.Services;

/// <summary>
/// Factory for creating and caching per-tenant MassTransit bus instances.
/// Each tenant has its own Azure Service Bus connection.
/// </summary>
public interface ITenantBusFactory : IAsyncDisposable
{
    /// <summary>
    /// Gets or creates a bus instance for the specified tenant.
    /// Bus instances are cached and reused.
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <returns>The bus control for the tenant</returns>
    IBus GetBus(Guid tenantId);
    
    /// <summary>
    /// Checks if a bus exists for the specified tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <returns>True if a bus is cached for this tenant</returns>
    bool HasBus(Guid tenantId);
}

