using GovUK.Dfe.FlexForms.Domain.ValueObjects;

namespace GovUK.Dfe.FlexForms.Application.Services;

/// <summary>
/// Provides the set of templates that belong to the current tenant.
/// The catalogue combines legacy configured mappings with templates explicitly
/// owned by the tenant. An isolated legacy database is used as a fallback when
/// neither source contains templates.
/// </summary>
public interface ITenantTemplateCatalogue
{
    /// <summary>
    /// Returns all template IDs that belong to the current tenant.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<TemplateId>> GetTemplateIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true when the template belongs to the current tenant catalogue.
    /// </summary>
    /// <param name="templateId">Template identifier to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> ContainsAsync(TemplateId templateId, CancellationToken cancellationToken = default);
}
