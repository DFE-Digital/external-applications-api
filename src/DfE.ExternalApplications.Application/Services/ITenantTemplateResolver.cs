using DfE.ExternalApplications.Domain.ValueObjects;

namespace DfE.ExternalApplications.Application.Services;

/// <summary>
/// Resolves application template IDs that belong to the current tenant.
/// </summary>
public interface ITenantTemplateResolver
{
    /// <summary>
    /// Returns all template IDs configured for the current tenant.
    /// </summary>
    IReadOnlyList<TemplateId> GetTemplateIdsForCurrentTenant();

    /// <summary>
    /// Returns true when the template belongs to the current tenant configuration.
    /// </summary>
    bool IsTemplateInCurrentTenant(TemplateId templateId);

    /// <summary>
    /// Resolves which template(s) to apply when listing applications.
    /// When <paramref name="requestedTemplateId"/> is set, returns that template only if it belongs to the tenant.
    /// Otherwise returns all templates configured for the tenant.
    /// </summary>
    IReadOnlyList<TemplateId> ResolveListingTemplateFilter(Guid? requestedTemplateId);
}
