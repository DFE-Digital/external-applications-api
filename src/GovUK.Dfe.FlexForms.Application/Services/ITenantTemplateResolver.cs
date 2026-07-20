using GovUK.Dfe.FlexForms.Domain.ValueObjects;

namespace GovUK.Dfe.FlexForms.Application.Services;

/// <summary>
/// Resolves application template IDs that belong to the current tenant.
/// </summary>
public interface ITenantTemplateResolver
{
    /// <summary>
    /// Returns all template IDs configured for the current tenant.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<TemplateId>> GetTemplateIdsForCurrentTenantAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true when the template belongs to the current tenant configuration.
    /// </summary>
    /// <param name="templateId">Template identifier to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> IsTemplateInCurrentTenantAsync(
        TemplateId templateId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves which template(s) to apply when listing applications without a user permission intersect.
    /// Prefer <see cref="IUserAccessibleTemplateService"/> when filtering for a specific user.
    /// When <paramref name="requestedTemplateId"/> is set, returns that template only if it belongs to the tenant.
    /// Otherwise returns all templates configured for the tenant.
    /// </summary>
    /// <param name="requestedTemplateId">Optional explicit template filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<TemplateId>> ResolveListingTemplateFilterAsync(
        Guid? requestedTemplateId,
        CancellationToken cancellationToken = default);
}
