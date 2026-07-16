using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;

namespace DfE.ExternalApplications.Application.Services;

/// <summary>
/// Resolves which tenant templates the current user is allowed to access
/// (tenant catalogue intersected with the user's template permissions).
/// </summary>
public interface IUserAccessibleTemplateService
{
    /// <summary>
    /// Returns template IDs the user may access within the current tenant.
    /// </summary>
    /// <param name="templatePermissions">The user's template permissions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<TemplateId>> GetAccessibleTemplateIdsAsync(
        IEnumerable<TemplatePermission> templatePermissions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves listing filters: when <paramref name="requestedTemplateId"/> is set,
    /// returns that template only if the user can access it; otherwise returns all accessible templates.
    /// </summary>
    /// <param name="templatePermissions">The user's template permissions.</param>
    /// <param name="requestedTemplateId">Optional explicit template filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<TemplateId>> ResolveAccessibleListingFilterAsync(
        IEnumerable<TemplatePermission> templatePermissions,
        Guid? requestedTemplateId,
        CancellationToken cancellationToken = default);
}
