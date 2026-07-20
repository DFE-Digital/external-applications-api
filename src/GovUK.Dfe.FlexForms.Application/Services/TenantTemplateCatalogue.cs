using GovUK.Dfe.FlexForms.Application.Templates.QueryObjects;
using GovUK.Dfe.FlexForms.Domain.Entities;
using GovUK.Dfe.FlexForms.Domain.Interfaces.Repositories;
using GovUK.Dfe.FlexForms.Domain.Tenancy;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GovUK.Dfe.FlexForms.Application.Services;

/// <inheritdoc />
public sealed class TenantTemplateCatalogue(
    IEaRepository<Template> templateRepository,
    ITenantContextAccessor tenantContextAccessor,
    ILogger<TenantTemplateCatalogue> logger) : ITenantTemplateCatalogue
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<TemplateId>> GetTemplateIdsAsync(CancellationToken cancellationToken = default)
    {
        var tenant = tenantContextAccessor.CurrentTenant;
        if (tenant is null)
        {
            logger.LogWarning("No current tenant when resolving tenant template catalogue.");
            return Array.Empty<TemplateId>();
        }

        // Configured mappings are the authoritative tenant membership list (1..N templates).
        // HostMappings must only contain this tenant's template GUIDs — shared EA databases
        // plus cross-tenant HostMappings would otherwise leak other tenants' templates.
        var fromConfig = ReadConfiguredTemplateIds(tenant);
        var ownedTemplateIds = await templateRepository.Query()
            .AsNoTracking()
            .Where(template => template.TenantId == tenant.Id && template.Id != null)
            .Select(template => template.Id!)
            .ToListAsync(cancellationToken);

        if (fromConfig.Count > 0 || ownedTemplateIds.Count > 0)
        {
            var tenantTemplateIds = fromConfig
                .Concat(ownedTemplateIds)
                .Distinct()
                .ToList()
                .AsReadOnly();

            logger.LogDebug(
                "Tenant {TenantName} catalogue resolved from configuration and owned templates ({Count} template(s)).",
                tenant.Name,
                tenantTemplateIds.Count);
            return tenantTemplateIds;
        }

        // Isolated tenant DB / legacy: no mappings or tenant-owned rows are configured,
        // so all templates in this database belong to the current tenant.
        var fromDatabase = await new GetAllTemplateIdsQueryObject()
            .Apply(templateRepository.Query().AsNoTracking())
            .ToListAsync(cancellationToken);

        logger.LogDebug(
            "Tenant {TenantName} catalogue resolved from database ({Count} template(s)); no HostMappings configured.",
            tenant.Name,
            fromDatabase.Count);

        return fromDatabase;
    }

    /// <inheritdoc />
    public async Task<bool> ContainsAsync(TemplateId templateId, CancellationToken cancellationToken = default)
    {
        var catalogue = await GetTemplateIdsAsync(cancellationToken);
        return catalogue.Any(t => t == templateId);
    }

    private IReadOnlyList<TemplateId> ReadConfiguredTemplateIds(TenantConfiguration tenant)
    {
        var templateIds = new List<TemplateId>();

        AddMappedIds(
            tenant,
            templateIds,
            "ApplicationTemplates:HostMappings",
            "ApplicationTemplates:HostMappings");
        AddMappedIds(
            tenant,
            templateIds,
            "Template:HostMappings",
            "Template:HostMappings");

        AddSingleId(tenant, templateIds, "ApplicationTemplates:TemplateId");
        AddSingleId(tenant, templateIds, "Template:Id");

        return templateIds
            .Distinct()
            .ToList()
            .AsReadOnly();
    }

    private void AddMappedIds(
        TenantConfiguration tenant,
        List<TemplateId> templateIds,
        string sectionPath,
        string logLabel)
    {
        foreach (var child in tenant.Settings.GetSection(sectionPath).GetChildren())
        {
            var value = child.Value;
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (Guid.TryParse(value, out var templateGuid))
            {
                templateIds.Add(new TemplateId(templateGuid));
                continue;
            }

            logger.LogWarning(
                "Ignoring invalid template GUID in {Section} for tenant {TenantName}. RawValue={RawValue}",
                logLabel,
                tenant.Name,
                value);
        }
    }

    private void AddSingleId(
        TenantConfiguration tenant,
        List<TemplateId> templateIds,
        string key)
    {
        var value = tenant.Settings[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (Guid.TryParse(value, out var templateGuid))
        {
            templateIds.Add(new TemplateId(templateGuid));
            return;
        }

        logger.LogWarning(
            "Ignoring invalid template GUID in {Key} for tenant {TenantName}. RawValue={RawValue}",
            key,
            tenant.Name,
            value);
    }
}
