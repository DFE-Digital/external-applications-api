using DfE.ExternalApplications.Domain.Tenancy;
using Microsoft.Extensions.Configuration;

namespace DfE.ExternalApplications.Tests.Common.Helpers;

/// <summary>
/// Provides tenant configuration from the customization (in-memory) so integration tests
/// do not depend on appsettings. Use for tests that need tenant-specific settings (e.g. FileStorage:Local).
/// </summary>
public sealed class TestTenantConfigurationProvider : ITenantConfigurationProvider
{
    private readonly IReadOnlyCollection<TenantConfiguration> _tenants;

    public TestTenantConfigurationProvider(string testTenantId, string tenantName = "Transfers")
    {
        var tenantId = Guid.Parse(testTenantId);
        var settings = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "FileStorage:Local:BaseDirectory", "/uploads" },
                { "FileStorage:Local:AllowedExtensions:0", "jpg" },
                { "FileStorage:Local:AllowedExtensions:1", "png" },
                { "FileStorage:Local:AllowedExtensions:2", "pdf" },
                { "FileStorage:Local:AllowedExtensions:3", "docx" },
                { "FileStorage:Local:AllowedExtensions:4", "xlsx" },
            })
            .Build();
        var tenant = new TenantConfiguration(
            tenantId,
            tenantName,
            settings,
            new[] { "https://localhost:7020" });
        _tenants = new[] { tenant };
    }

    public TenantConfiguration? GetTenant(Guid id)
        => _tenants.FirstOrDefault(t => t.Id == id);

    public IReadOnlyCollection<TenantConfiguration> GetAllTenants()
        => _tenants;
}
