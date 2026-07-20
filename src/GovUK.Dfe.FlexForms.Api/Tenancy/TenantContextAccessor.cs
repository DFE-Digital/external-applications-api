using GovUK.Dfe.FlexForms.Domain.Tenancy;

namespace GovUK.Dfe.FlexForms.Api.Tenancy;

public class TenantContextAccessor : ITenantContextAccessor
{
    public TenantConfiguration? CurrentTenant { get; set; }
}
