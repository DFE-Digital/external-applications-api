using GovUK.Dfe.FlexForms.Domain.Entities;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;

namespace GovUK.Dfe.FlexForms.Application.Services;

/// <inheritdoc />
public sealed class UserAccessibleTemplateService(
    ITenantTemplateCatalogue tenantTemplateCatalogue) : IUserAccessibleTemplateService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<TemplateId>> GetAccessibleTemplateIdsAsync(
        IEnumerable<TemplatePermission> templatePermissions,
        CancellationToken cancellationToken = default)
    {
        var catalogue = await tenantTemplateCatalogue.GetTemplateIdsAsync(cancellationToken);
        if (catalogue.Count == 0)
        {
            return Array.Empty<TemplateId>();
        }

        var permitted = templatePermissions
            .Select(tp => tp.TemplateId)
            .Distinct()
            .ToHashSet();

        return catalogue
            .Where(permitted.Contains)
            .ToList()
            .AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TemplateId>> ResolveAccessibleListingFilterAsync(
        IEnumerable<TemplatePermission> templatePermissions,
        Guid? requestedTemplateId,
        CancellationToken cancellationToken = default)
    {
        var accessible = await GetAccessibleTemplateIdsAsync(templatePermissions, cancellationToken);
        if (accessible.Count == 0)
        {
            return Array.Empty<TemplateId>();
        }

        if (!requestedTemplateId.HasValue)
        {
            return accessible;
        }

        var requested = new TemplateId(requestedTemplateId.Value);
        return accessible.Contains(requested)
            ? new[] { requested }
            : Array.Empty<TemplateId>();
    }
}
