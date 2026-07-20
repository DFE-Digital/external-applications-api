using GovUK.Dfe.FlexForms.Domain.Tenancy;
using GovUK.Dfe.CoreLibs.Security.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GovUK.Dfe.FlexForms.Api.Security;

/// <summary>
/// Binds named <see cref="TokenSettings"/> from the live tenant configuration snapshot
/// so internal JWT minting always uses the same secrets as TenantBearer validation.
/// The options name is the tenant id (<see cref="Guid"/> string).
/// </summary>
public sealed class TenantTokenSettingsConfigurator(
    ITenantConfigurationProvider tenantConfigurationProvider,
    ILogger<TenantTokenSettingsConfigurator> logger)
    : IConfigureNamedOptions<TokenSettings>
{
    /// <inheritdoc />
    public void Configure(TokenSettings options) => Configure(Options.DefaultName, options);

    /// <inheritdoc />
    public void Configure(string? name, TokenSettings options)
    {
        if (string.IsNullOrWhiteSpace(name) ||
            string.Equals(name, Options.DefaultName, StringComparison.Ordinal) ||
            !Guid.TryParse(name, out var tenantId))
        {
            return;
        }

        var tenant = tenantConfigurationProvider.GetTenant(tenantId);
        if (tenant is null)
        {
            logger.LogWarning(
                "TokenSettingsconfigure: tenant {TenantId} not found in configuration provider.",
                tenantId);
            return;
        }

        // Prefer flat keys (matches FlattenJson from TenantConfig DB) over Bind alone.
        var settings = tenant.Settings;
        var secretKey = settings["Authorization:TokenSettings:SecretKey"];
        var issuer = settings["Authorization:TokenSettings:Issuer"];
        var audience = settings["Authorization:TokenSettings:Audience"];

        if (string.IsNullOrEmpty(secretKey))
        {
            var section = settings.GetSection("Authorization:TokenSettings");
            if (section.Exists())
            {
                section.Bind(options);
                secretKey = options.SecretKey;
                issuer = string.IsNullOrEmpty(issuer) ? options.Issuer : issuer;
                audience = string.IsNullOrEmpty(audience) ? options.Audience : audience;
            }
        }

        if (string.IsNullOrEmpty(secretKey))
        {
            logger.LogError(
                "TokenSettings for tenant {TenantId} ({TenantName}) has empty SecretKey. " +
                "Ensure Authorization:TokenSettings is imported for Target=Api.",
                tenantId,
                tenant.Name);
            return;
        }

        options.SecretKey = secretKey;
        options.Issuer = issuer ?? string.Empty;
        options.Audience = audience ?? string.Empty;

        if (int.TryParse(settings["Authorization:TokenSettings:TokenLifetimeMinutes"], out var lifetime))
        {
            options.TokenLifetimeMinutes = lifetime;
        }

        if (int.TryParse(settings["Authorization:TokenSettings:BufferInSeconds"], out var buffer))
        {
            options.BufferInSeconds = buffer;
        }
    }
}
