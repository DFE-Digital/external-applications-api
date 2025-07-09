using DfE.CoreLibs.Contracts.ExternalApplications.Enums;

namespace DfE.ExternalApplications.Domain.Services;

/// <summary>
/// Domain service for checking permissions based on claims.
/// This service operates on the domain level and is resource-type agnostic.
/// </summary>
public interface IPermissionCheckerService
{
    /// <summary>
    /// Checks if the current user has a specific permission for a resource
    /// </summary>
    /// <param name="resourceType">Type of the resource (e.g., Template, Application)</param>
    /// <param name="resourceId">Identifier of the resource</param>
    /// <param name="accessType">Type of access required</param>
    /// <returns>True if the user has the specified permission, false otherwise</returns>
    bool HasPermission(ResourceType resourceType, string resourceId, AccessType accessType);

    /// <summary>
    /// Checks if the current user has any permission of the specified type for any resource of the given type
    /// </summary>
    /// <param name="resourceType">Type of the resource (e.g., Template, Application)</param>
    /// <param name="accessType">Type of access required</param>
    /// <returns>True if the user has any matching permission, false otherwise</returns>
    bool HasAnyPermission(ResourceType resourceType, AccessType accessType);

    /// <summary>
    /// Gets all resource IDs of a specific type that the current user has permission to access
    /// </summary>
    /// <param name="resourceType">Type of the resource (e.g., Template, Application)</param>
    /// <param name="accessType">Type of access required</param>
    /// <returns>Collection of resource IDs the user has permission to access</returns>
    IReadOnlyCollection<string> GetResourceIdsWithPermission(ResourceType resourceType, AccessType accessType);

    /// <summary>
    /// Checks if the current user has a specific permission for a template
    /// </summary>
    /// <param name="templateId">Identifier of the template</param>
    /// <param name="accessType">Type of access required</param>
    /// <returns>True if the user has the specified permission, false otherwise</returns>
    bool HasTemplatePermission(string templateId, AccessType accessType);
}