namespace DfE.ExternalApplications.Domain.Tenancy;

public interface ITenantConfigurationProvider
{
    TenantConfiguration? GetTenant(Guid id);

    IReadOnlyCollection<TenantConfiguration> GetAllTenants();
}
