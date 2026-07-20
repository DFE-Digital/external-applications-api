namespace GovUK.Dfe.FlexForms.Domain.Tenancy.Entities;

/// <summary>
/// Represents a tenant in the tenant configuration database.
/// </summary>
public class TenantEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<TenantSettingEntity> Settings { get; set; } = new List<TenantSettingEntity>();

    public ICollection<TenantHostnameEntity> Hostnames { get; set; } = new List<TenantHostnameEntity>();

    public ICollection<TenantFrontendOriginEntity> FrontendOrigins { get; set; } = new List<TenantFrontendOriginEntity>();

    public ICollection<TenantPrincipalEntity> Principals { get; set; } = new List<TenantPrincipalEntity>();
}
