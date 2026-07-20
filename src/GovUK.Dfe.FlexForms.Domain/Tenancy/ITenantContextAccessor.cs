namespace GovUK.Dfe.FlexForms.Domain.Tenancy;

public interface ITenantContextAccessor
{
    TenantConfiguration? CurrentTenant { get; set; }
}
