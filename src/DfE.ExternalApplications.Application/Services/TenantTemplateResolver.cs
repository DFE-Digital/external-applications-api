using DfE.ExternalApplications.Domain.Tenancy;
using DfE.ExternalApplications.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DfE.ExternalApplications.Application.Services;

/// <inheritdoc />
public sealed class TenantTemplateResolver(
    ITenantContextAccessor tenantContextAccessor,
    ILogger<TenantTemplateResolver> logger) : ITenantTemplateResolver
{
    /// <inheritdoc />
    public IReadOnlyList<TemplateId> GetTemplateIdsForCurrentTenant()
    {
        var tenant = tenantContextAccessor.CurrentTenant;
        if (tenant is null)
        {
            logger.LogWarning("No current tenant when resolving application templates.");
            return Array.Empty<TemplateId>();
        }

        var hostMappings = tenant.Settings.GetSection("ApplicationTemplates:HostMappings")
            .GetChildren()
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToList();

        var templateIds = new List<TemplateId>();
        foreach (var value in hostMappings)
        {
            if (Guid.TryParse(value, out var templateGuid))
            {
                templateIds.Add(new TemplateId(templateGuid));
                continue;
            }

            logger.LogWarning(
                "Ignoring invalid template GUID in ApplicationTemplates:HostMappings for tenant {TenantName}. RawValue={RawValue}",
                tenant.Name,
                value);
        }

        return templateIds
            .Distinct()
            .ToList()
            .AsReadOnly();
    }

    /// <inheritdoc />
    public bool IsTemplateInCurrentTenant(TemplateId templateId) =>
        GetTemplateIdsForCurrentTenant().Any(t => t == templateId);

    /// <inheritdoc />
    public IReadOnlyList<TemplateId> ResolveListingTemplateFilter(Guid? requestedTemplateId)
    {
        var tenantTemplateIds = GetTemplateIdsForCurrentTenant();
        if (tenantTemplateIds.Count == 0)
            return Array.Empty<TemplateId>();

        if (!requestedTemplateId.HasValue)
            return tenantTemplateIds;

        var requested = new TemplateId(requestedTemplateId.Value);
        return tenantTemplateIds.Contains(requested)
            ? new[] { requested }
            : Array.Empty<TemplateId>();
    }
}
