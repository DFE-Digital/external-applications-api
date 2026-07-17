using DfE.ExternalApplications.Domain.ValueObjects;

namespace DfE.ExternalApplications.Application.Services;

/// <summary>
/// Provides the set of templates that belong to the current tenant.
/// When HostMappings (or Template:Id) are configured, those GUIDs are the
/// authoritative catalogue. Otherwise all templates in the tenant EA database
/// are treated as belonging to the current tenant (isolated DB / legacy).
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
