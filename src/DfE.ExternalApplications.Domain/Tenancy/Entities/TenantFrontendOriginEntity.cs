namespace DfE.ExternalApplications.Domain.Tenancy.Entities;

/// <summary>
/// Maps a frontend origin URL to a tenant for origin-based tenant resolution in the API.
/// </summary>
public class TenantFrontendOriginEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string Origin { get; set; } = null!;

    public TenantEntity Tenant { get; set; } = null!;
}
