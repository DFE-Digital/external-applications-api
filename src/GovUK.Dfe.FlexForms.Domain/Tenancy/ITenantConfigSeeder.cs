namespace GovUK.Dfe.FlexForms.Domain.Tenancy;

/// <summary>
/// Seeds the tenant configuration store from the application's current configuration source.
/// </summary>
public interface ITenantConfigSeeder
{
    /// <summary>
    /// Seeds tenant configuration data from appsettings into the tenant config store.
    /// Skips seeding if data already exists.
    /// </summary>
    Task SeedFromAppSettingsAsync(CancellationToken cancellationToken = default);
}
