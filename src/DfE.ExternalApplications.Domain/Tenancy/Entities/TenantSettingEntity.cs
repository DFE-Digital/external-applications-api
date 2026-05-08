namespace DfE.ExternalApplications.Domain.Tenancy.Entities;

/// <summary>
/// Stores a category of configuration settings for a tenant as a JSON blob.
/// Target determines which application consumes the setting: Shared, Api, or Web.
/// </summary>
public class TenantSettingEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    /// <summary>
    /// The configuration category, e.g. "ConnectionStrings", "DfESignIn", "AzureAd", "Layout".
    /// </summary>
    public string Category { get; set; } = null!;

    /// <summary>
    /// Which application consumes this setting: "Shared", "Api", or "Web".
    /// </summary>
    public string Target { get; set; } = "Shared";

    /// <summary>
    /// JSON blob containing the configuration for this category.
    /// </summary>
    public string Settings { get; set; } = null!;

    /// <summary>
    /// Indicates whether this setting contains sensitive values (connection strings, secrets).
    /// </summary>
    public bool IsSecret { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public TenantEntity Tenant { get; set; } = null!;
}
