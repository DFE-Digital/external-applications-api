using System.Collections.Generic;
using System.Linq;
using DfE.ExternalApplications.Domain.Tenancy;
using Microsoft.Extensions.Configuration;

namespace DfE.ExternalApplications.Infrastructure.Services;

public class OptionsTenantConfigurationProvider(IConfiguration configuration) : ITenantConfigurationProvider
{
    private readonly IReadOnlyDictionary<Guid, TenantConfiguration> _tenants = BuildAllTenants(configuration);

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
            var tenantIdString = section["Id"];
            
            if (string.IsNullOrWhiteSpace(tenantIdString))
            {
                throw new InvalidOperationException(
                    $"Tenant '{section.Key}' is missing required 'Id' field. Each tenant must have a unique GUID as its Id.");
            }
            
            if (!Guid.TryParse(tenantIdString, out var tenantId))
            {
                throw new InvalidOperationException(
                    $"Tenant '{section.Key}' has invalid Id '{tenantIdString}'. The Id must be a valid GUID.");
            }
            
            if (result.ContainsKey(tenantId))
            {
                throw new InvalidOperationException(
                    $"Duplicate tenant Id '{tenantId}' found. Each tenant must have a unique Id.");
            }

            var tenant = BuildTenantConfiguration(section, tenantId);
            result[tenantId] = tenant;
        }

        return result;
    }

    private static TenantConfiguration BuildTenantConfiguration(IConfigurationSection tenantSection, Guid tenantId)
    {
        var name = tenantSection.GetValue<string>("Name");
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException($"Tenant '{tenantSection.Key}' (Id: {tenantId}) must have a non-empty Name.");
        }

        // Flatten the tenant section so consumers can read settings without the Tenants:{id} prefix
        var flattenedPairs = new List<KeyValuePair<string, string?>>();
        var prefix = tenantSection.Path + ":";
        
        FlattenSection(tenantSection, prefix, flattenedPairs);

        var settings = new ConfigurationBuilder()
            .AddInMemoryCollection(flattenedPairs!)
            .Build();

        var origins = ResolveFrontendOrigins(settings);

        return new TenantConfiguration(tenantId, name!, settings, origins);
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
