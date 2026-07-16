using DfE.ExternalApplications.Domain.ValueObjects;

namespace DfE.ExternalApplications.Application.Services;

/// <inheritdoc />
public sealed class TenantTemplateResolver(
    ITenantTemplateCatalogue tenantTemplateCatalogue) : ITenantTemplateResolver
{
    /// <inheritdoc />
    public Task<IReadOnlyList<TemplateId>> GetTemplateIdsForCurrentTenantAsync(
        CancellationToken cancellationToken = default)
        => tenantTemplateCatalogue.GetTemplateIdsAsync(cancellationToken);

    /// <inheritdoc />
    public Task<bool> IsTemplateInCurrentTenantAsync(
        TemplateId templateId,
        CancellationToken cancellationToken = default)
        => tenantTemplateCatalogue.ContainsAsync(templateId, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<TemplateId>> ResolveListingTemplateFilterAsync(
        Guid? requestedTemplateId,
        CancellationToken cancellationToken = default)
    {
        var tenantTemplateIds = await GetTemplateIdsForCurrentTenantAsync(cancellationToken);
        if (tenantTemplateIds.Count == 0)
        {
            return Array.Empty<TemplateId>();
        }

        if (!requestedTemplateId.HasValue)
        {
            return tenantTemplateIds;
        }

        var requested = new TemplateId(requestedTemplateId.Value);
        return tenantTemplateIds.Contains(requested)
            ? new[] { requested }
            : Array.Empty<TemplateId>();
    }
}
