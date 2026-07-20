namespace GovUK.Dfe.FlexForms.Domain.Tenancy.Entities;

/// <summary>
/// Maps a hostname to a tenant for hostname-based tenant resolution.
/// </summary>
public class TenantHostnameEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string Hostname { get; set; } = null!;

    public TenantEntity Tenant { get; set; } = null!;
}
