using System.Collections.Generic;
using System.Linq;
using GovUK.Dfe.FlexForms.Domain.Tenancy;
using Microsoft.Extensions.Configuration;

namespace GovUK.Dfe.FlexForms.Infrastructure.Services;

public class OptionsTenantConfigurationProvider : ITenantConfigurationProvider
{
    private readonly IReadOnlyDictionary<Guid, TenantConfiguration> _tenants;
    private readonly IReadOnlyDictionary<string, TenantConfiguration> _tenantsByOrigin;

    public OptionsTenantConfigurationProvider(IConfiguration configuration)
    {
        _tenants = BuildAllTenants(configuration);
        _tenantsByOrigin = BuildOriginIndex(_tenants.Values);
    }

    public string Source => "AppSettings";

    public TenantConfiguration? GetTenant(Guid id)
        => _tenants.TryGetValue(id, out var tenant) ? tenant : null;

    public TenantConfiguration? GetTenantByOrigin(string origin)
        => _tenantsByOrigin.TryGetValue(origin, out var tenant) ? tenant : null;

    public IReadOnlyCollection<TenantConfiguration> GetAllTenants()
        => _tenants.Values.ToArray();

    private static IReadOnlyDictionary<string, TenantConfiguration> BuildOriginIndex(
        IEnumerable<TenantConfiguration> tenants)
    {
        var result = new Dictionary<string, TenantConfiguration>(StringComparer.OrdinalIgnoreCase);
        foreach (var tenant in tenants)
        {
            foreach (var origin in tenant.FrontendOrigins)
            {
                result[origin] = tenant;
            }
        }
        return result;
    }

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
