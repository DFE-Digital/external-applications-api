namespace DfE.ExternalApplications.Domain.Tenancy;

/// <summary>
/// Reads tenant configuration directly from the persistent store (bypasses the in-memory cache),
/// filtered by the requested target. Secret settings are decrypted before returning.
/// Used by the consume endpoint so callers always get the freshest config for their target.
/// </summary>
public interface ITenantSettingsReader
{
    /// <summary>
    /// Loads merged 'Shared' + the requested target ('Web' | 'Api') settings for a tenant.
    /// Returned as flat key-value pairs using the ':' separator (compatible with IConfiguration).
    /// </summary>
    /// <param name="tenantId">The tenant id resolved from the authenticated principal.</param>
    /// <param name="target">The consuming application's target ('Web' or 'Api'). 'Shared' is always included.</param>
    Task<TenantConfigurationSnapshot?> GetConfigurationAsync(
        Guid tenantId,
        string target,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// A point-in-time snapshot of a tenant's merged configuration.
/// </summary>
public sealed record TenantConfigurationSnapshot(
    Guid TenantId,
    string TenantName,
    DateTime LoadedAtUtc,
    IReadOnlyDictionary<string, string?> Configuration);
