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
}
