using DfE.ExternalApplications.Domain.ValueObjects;

namespace DfE.ExternalApplications.Domain.Services;

public interface ITemplatePermissionService
{
    /// <summary>
    /// Checks if a user has permission to create an application for a template
    /// </summary>
    /// <param name="principalId">User's email or external provider ID</param>
    /// <param name="templateId">Template ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the user has permission, false otherwise</returns>
    Task<bool> CanUserCreateApplicationForTemplate(string principalId, Guid templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has permission to create an application for a template using user ID directly
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="templateId">Template ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the user has permission, false otherwise</returns>
    Task<bool> CanUserCreateApplicationForTemplate(UserId userId, Guid templateId, CancellationToken cancellationToken = default);
} 