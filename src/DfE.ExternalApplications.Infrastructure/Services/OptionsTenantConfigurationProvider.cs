using System.Collections.Generic;
using System.Linq;
using DfE.ExternalApplications.Domain.Tenancy;
using Microsoft.Extensions.Configuration;

namespace DfE.ExternalApplications.Infrastructure.Services;

public class OptionsTenantConfigurationProvider : ITenantConfigurationProvider
{
    private readonly IConfiguration _configuration;

    public OptionsTenantConfigurationProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public TenantConfiguration? GetTenant(Guid id)
    {
        var tenantSection = _configuration.GetSection("Tenants").GetSection(id.ToString());
        if (!tenantSection.Exists())
        {
            return null;
        }

        return BuildTenantConfiguration(tenantSection, id);
    }

    public IReadOnlyCollection<TenantConfiguration> GetAllTenants()
    {
        var tenantsSection = _configuration.GetSection("Tenants");
        return tenantsSection
            .GetChildren()
            .Select(section => Guid.TryParse(section.Key, out var key)
                ? BuildTenantConfiguration(section, key)
                : null)
            .Where(t => t is not null)
            .Select(t => t!)
            .ToArray();
    }

    private static TenantConfiguration BuildTenantConfiguration(IConfigurationSection tenantSection, Guid key)
    {
        var configuredId = tenantSection.GetValue<Guid?>("Id") ?? key;
        if (configuredId != key)
        {
            throw new InvalidOperationException($"Tenant key {key} does not match configured Id {configuredId}.");
        }

        var name = tenantSection.GetValue<string>("Name");
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException($"Tenant {key} must have a non-empty Name.");
        }

        // Flatten the tenant section so consumers can read settings without the Tenants:{id} prefix
        var flattenedPairs = tenantSection.AsEnumerable(makePaths: true)
            .Where(kv => kv.Value is not null && !string.Equals(kv.Key, tenantSection.Path, StringComparison.OrdinalIgnoreCase))
            .Select(kv =>
            {
                var prefix = $"{tenantSection.Path}:";
                var trimmedKey = kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                    ? kv.Key[prefix.Length..]
                    : kv.Key;
                return new KeyValuePair<string, string?>(trimmedKey, kv.Value);
            });

        var settings = new ConfigurationBuilder()
            .AddInMemoryCollection(flattenedPairs!)
            .Build();

        var origins = ResolveFrontendOrigins(settings);

        return new TenantConfiguration(configuredId, name!, settings, origins);
    }

    private static string[] ResolveFrontendOrigins(IConfiguration settings)
    {
        var configuredOrigins = settings.GetSection("Frontend:Origins").Get<string[]>();
        if (configuredOrigins is { Length: > 0 })
        {
            return configuredOrigins;
        }

        var singleOrigin = settings["Frontend:Origin"];
        return string.IsNullOrWhiteSpace(singleOrigin) ? Array.Empty<string>() : new[] { singleOrigin };
    }
}
