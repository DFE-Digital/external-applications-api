namespace DfE.ExternalApplications.Domain.Tenancy.Entities;

/// <summary>
/// Maps an authentication principal (Managed Identity object id, service principal id, or API key id)
/// to a tenant. Used by the tenant configuration consume endpoint to resolve which tenant the
/// caller belongs to without trusting any client-supplied tenant identifier.
/// </summary>
public class TenantPrincipalEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    /// <summary>
    /// The principal's identifier as it appears in the inbound credential.
    /// For Azure AD this is typically the 'oid' claim (object id) of a Managed Identity or Service Principal.
    /// </summary>
    public string PrincipalObjectId { get; set; } = null!;

    /// <summary>
    /// Type of principal: "ManagedIdentity", "ServicePrincipal", or "ApiKey".
    /// Informational; allows future logic per principal kind.
    /// </summary>
    public string PrincipalType { get; set; } = "ManagedIdentity";

    /// <summary>
    /// Optional human-friendly description (e.g. "tenant-xyz Web container - PROD").
    /// </summary>
    public string? DisplayName { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public TenantEntity Tenant { get; set; } = null!;
}
