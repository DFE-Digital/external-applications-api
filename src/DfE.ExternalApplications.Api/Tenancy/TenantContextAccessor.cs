using DfE.ExternalApplications.Domain.Tenancy;

namespace DfE.ExternalApplications.Api.Tenancy;

public class TenantContextAccessor : ITenantContextAccessor
{
    public TenantConfiguration? CurrentTenant { get; set; }
}
