using Microsoft.Extensions.Configuration;

namespace DfE.ExternalApplications.Domain.Tenancy;

public class TenantConfiguration
{
    public TenantConfiguration(Guid id, string name, IConfigurationRoot settings, string[] frontendOrigins)
    {
        Id = id;
        Name = name;
        Settings = settings;
        FrontendOrigins = frontendOrigins;
    }

    public Guid Id { get; }

    public string Name { get; }

    /// <summary>
    /// Full tenant-specific settings, flattened so consumers can access keys
    /// exactly as they existed before the Tenants wrapper was introduced.
    /// </summary>
    public IConfigurationRoot Settings { get; }

    public string[] FrontendOrigins { get; }

    public string? GetConnectionString(string name) => Settings.GetConnectionString(name);
}
