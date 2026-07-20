using System.Text.Json;
using GovUK.Dfe.FlexForms.Domain.Tenancy;
using GovUK.Dfe.FlexForms.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GovUK.Dfe.FlexForms.Infrastructure.Services;

/// <summary>
/// Loads merged 'Shared' + target-specific settings for a tenant directly from the database,
/// decrypting secret categories before flattening into IConfiguration-compatible key/value pairs.
/// </summary>
public class TenantSettingsReader(
    TenantConfigDbContext dbContext,
    ITenantSettingsEncryptor encryptor,
    ILogger<TenantSettingsReader> logger) : ITenantSettingsReader
{
    /// <inheritdoc />
    public async Task<TenantConfigurationSnapshot?> GetConfigurationAsync(
        Guid tenantId,
        string target,
        CancellationToken cancellationToken = default)
    {
        var tenant = await dbContext.Tenants
            .AsNoTracking()
            .Where(t => t.Id == tenantId && t.IsActive)
            .Select(t => new { t.Id, t.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (tenant is null)
            return null;

        var settings = await dbContext.TenantSettings
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId &&
                        (s.Target == "Shared" || s.Target == target))
            .OrderBy(s => s.Target)
            .ThenBy(s => s.Category)
            .ToListAsync(cancellationToken);

        var configuration = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var setting in settings)
        {
            try
            {
                var json = setting.IsSecret
                    ? encryptor.Decrypt(setting.Settings)
                    : setting.Settings;

                FlattenJson(setting.Category, json, configuration);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to read setting '{Category}' (Target={Target}) for tenant '{TenantName}'. Skipping.",
                    setting.Category, setting.Target, tenant.Name);
            }
        }

        return new TenantConfigurationSnapshot(
            tenant.Id,
            tenant.Name,
            DateTime.UtcNow,
            configuration);
    }

    private static void FlattenJson(
        string category,
        string json,
        IDictionary<string, string?> result)
    {
        using var doc = JsonDocument.Parse(json);
        FlattenJsonElement(category, doc.RootElement, result);
    }

    private static void FlattenJsonElement(
        string prefix,
        JsonElement element,
        IDictionary<string, string?> result)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    FlattenJsonElement($"{prefix}:{property.Name}", property.Value, result);
                }
                break;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    FlattenJsonElement($"{prefix}:{index}", item, result);
                    index++;
                }
                break;

            case JsonValueKind.Null:
                result[prefix] = null;
                break;

            default:
                result[prefix] = element.ToString();
                break;
        }
    }
}
