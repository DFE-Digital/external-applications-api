using System.Collections.Generic;
using System.Linq;
using DfE.ExternalApplications.Domain.Tenancy;
using Microsoft.Extensions.Configuration;

namespace DfE.ExternalApplications.Infrastructure.Services;

public class OptionsTenantConfigurationProvider : ITenantConfigurationProvider
{
    private readonly IReadOnlyDictionary<Guid, TenantConfiguration> _tenants;

    public OptionsTenantConfigurationProvider(IConfiguration configuration)
    {
        _tenants = BuildAllTenants(configuration);
    }

    public TenantConfiguration? GetTenant(Guid id)
        => _tenants.TryGetValue(id, out var tenant) ? tenant : null;

    public IReadOnlyCollection<TenantConfiguration> GetAllTenants()
        => _tenants.Values.ToArray();

    private static IReadOnlyDictionary<Guid, TenantConfiguration> BuildAllTenants(IConfiguration configuration)
    {
        var tenantsSection = configuration.GetSection("Tenants");
        var result = new Dictionary<Guid, TenantConfiguration>();

        foreach (var section in tenantsSection.GetChildren())
        {
            if (Guid.TryParse(section.Key, out var key))
            {
                var tenant = BuildTenantConfiguration(section, key);
                result[key] = tenant;
            }
        }

        return result;
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
        var flattenedPairs = new List<KeyValuePair<string, string?>>();
        var prefix = tenantSection.Path + ":";
        
        FlattenSection(tenantSection, prefix, flattenedPairs);

        var settings = new ConfigurationBuilder()
            .AddInMemoryCollection(flattenedPairs!)
            .Build();

        var origins = ResolveFrontendOrigins(settings);

        return new TenantConfiguration(configuredId, name!, settings, origins);
    }

    private static void FlattenSection(
        IConfigurationSection section, 
        string prefixToRemove, 
        List<KeyValuePair<string, string?>> result)
    {
        foreach (var child in section.GetChildren())
        {
            if (child.Value != null)
            {
                var trimmedKey = child.Path.StartsWith(prefixToRemove, StringComparison.OrdinalIgnoreCase)
                    ? child.Path[prefixToRemove.Length..]
                    : child.Path;
                result.Add(new KeyValuePair<string, string?>(trimmedKey, child.Value));
            }
            
            // Recursively process child sections
            FlattenSection(child, prefixToRemove, result);
        }
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
