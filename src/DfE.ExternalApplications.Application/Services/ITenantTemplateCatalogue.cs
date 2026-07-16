using DfE.ExternalApplications.Domain.ValueObjects;

namespace DfE.ExternalApplications.Application.Services;

/// <summary>
/// Provides the set of templates that belong to the current tenant.
/// Membership is driven from the tenant EA database (admin-created templates)
/// and remains compatible with configured HostMappings GUIDs.
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
