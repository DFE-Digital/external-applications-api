namespace DfE.ExternalApplications.Domain.Tenancy;

/// <summary>
/// Reads global host-level configuration for downstream applications (e.g. the Web container)
/// without requiring a tenant context.
/// </summary>
public interface IHostConfigurationReader
{
    /// <summary>
    /// Returns a Web-safe subset of root host configuration (global settings and allow-listed connection strings).
    /// </summary>
    /// <param name="target">The consuming application's target (e.g. Web).</param>
    HostConfigurationSnapshot GetConfiguration(string target);
}

/// <summary>
/// A point-in-time snapshot of global host configuration.
/// </summary>
public sealed record HostConfigurationSnapshot(
    string Target,
    DateTime LoadedAtUtc,
    IReadOnlyDictionary<string, string?> Configuration);
