using GovUK.Dfe.FlexForms.Domain.Tenancy;
using GovUK.Dfe.FlexForms.Infrastructure.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GovUK.Dfe.FlexForms.Infrastructure.Services;

/// <summary>
/// Wraps the static TenantConfigSeeder for dependency injection.
/// </summary>
public class TenantConfigSeederService(
    TenantConfigDbContext dbContext,
    IConfiguration configuration,
    ITenantSettingsEncryptor encryptor,
    ILogger<TenantConfigSeederService> logger) : ITenantConfigSeeder
{
    /// <inheritdoc />
    public Task SeedFromAppSettingsAsync(CancellationToken cancellationToken = default)
        => TenantConfigSeeder.SeedFromAppSettingsAsync(dbContext, configuration, encryptor, logger);
}
