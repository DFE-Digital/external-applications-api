using DfE.ExternalApplications.Application.Templates.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Tenancy;
using DfE.ExternalApplications.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DfE.ExternalApplications.Application.Services;

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

        var fromDatabase = await new GetAllTemplateIdsQueryObject()
            .Apply(templateRepository.Query().AsNoTracking())
            .ToListAsync(cancellationToken);

        var fromHostMappings = ReadHostMappingTemplateIds(tenant);

        return fromDatabase
            .Concat(fromHostMappings)
            .Distinct()
            .ToList()
            .AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<bool> ContainsAsync(TemplateId templateId, CancellationToken cancellationToken = default)
    {
        var catalogue = await GetTemplateIdsAsync(cancellationToken);
        return catalogue.Any(t => t == templateId);
    }

    private IReadOnlyList<TemplateId> ReadHostMappingTemplateIds(TenantConfiguration tenant)
    {
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

        return templateIds;
    }
}
