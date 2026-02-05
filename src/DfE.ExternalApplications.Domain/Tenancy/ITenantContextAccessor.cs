namespace DfE.ExternalApplications.Domain.Tenancy;

public interface ITenantContextAccessor
{
    TenantConfiguration? CurrentTenant { get; set; }
}
