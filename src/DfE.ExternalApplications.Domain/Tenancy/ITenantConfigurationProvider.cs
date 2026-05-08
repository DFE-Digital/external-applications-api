namespace DfE.ExternalApplications.Domain.Tenancy;

/// <summary>
/// Provides access to tenant configurations with O(1) lookups by id and origin.
/// </summary>
public interface ITenantConfigurationProvider
{
    /// <summary>
    /// Gets a tenant configuration by its unique identifier.
    /// </summary>
    TenantConfiguration? GetTenant(Guid id);

    /// <summary>
    /// Gets a tenant configuration by matching a frontend origin URL.
    /// </summary>
    TenantConfiguration? GetTenantByOrigin(string origin);

    /// <summary>
    /// Gets all configured tenants.
    /// </summary>
    IReadOnlyCollection<TenantConfiguration> GetAllTenants();

    /// <summary>
    /// Refreshes the tenant configuration cache from the underlying store.
    /// No-op for providers that do not support refresh (e.g. appsettings-based).
    /// </summary>
    Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>
    /// The source of the tenant configuration (e.g. "Database", "AppSettings").
    /// </summary>
    string Source => "Unknown";
}
