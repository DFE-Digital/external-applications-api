using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.ExternalApplications.Domain.Common;
using System.Security.Claims;

namespace DfE.ExternalApplications.Domain.Services;

/// <summary>
/// Evaluates resource permission claims on a <see cref="ClaimsPrincipal"/>, including role-based
/// capabilities and tenant-wide wildcard permissions.
/// </summary>
public static class PermissionClaimEvaluator
{
    public const string PermissionClaimType = "permission";

    /// <summary>
    /// Returns true when the user has full administrative access (all resources, all actions).
    /// </summary>
    public static bool HasFullAdminAccess(ClaimsPrincipal user) =>
        user.IsInRole(RoleNames.Admin);

    /// <summary>
    /// Returns true when the user can read all applications in the current tenant
    /// (Admin, Caseworker, or tenant-wide read wildcard).
    /// </summary>
    public static bool CanReadAllApplications(ClaimsPrincipal user) =>
        HasFullAdminAccess(user)
        || user.IsInRole(RoleNames.Caseworker)
        || HasPermissionClaim(user, ResourceType.Application, PermissionConstants.AnyResourceKey, AccessType.Read);

    /// <summary>
    /// Returns true when the user can write any application in the tenant (Admin only).
    /// </summary>
    public static bool CanWriteAnyApplication(ClaimsPrincipal user) =>
        HasFullAdminAccess(user);

    /// <summary>
    /// Returns true when the user can read the specified application.
    /// </summary>
    public static bool CanReadApplication(ClaimsPrincipal user, string applicationId) =>
        HasFullAdminAccess(user)
        || user.IsInRole(RoleNames.Caseworker)
        || HasPermissionClaim(user, ResourceType.Application, applicationId, AccessType.Read)
        || HasPermissionClaim(user, ResourceType.Application, PermissionConstants.AnyResourceKey, AccessType.Read);

    /// <summary>
    /// Returns true when the user can write the specified application (exact permission or Admin only).
    /// Wildcard write grants are intentionally excluded to avoid elevating standard users.
    /// </summary>
    public static bool CanWriteApplication(ClaimsPrincipal user, string applicationId) =>
        HasFullAdminAccess(user)
        || HasPermissionClaim(user, ResourceType.Application, applicationId, AccessType.Write);

    /// <summary>
    /// Returns true when the user can read files for the specified application.
    /// </summary>
    public static bool CanReadApplicationFiles(ClaimsPrincipal user, string applicationId) =>
        HasFullAdminAccess(user)
        || user.IsInRole(RoleNames.Caseworker)
        || HasPermissionClaim(user, ResourceType.ApplicationFiles, applicationId, AccessType.Read)
        || HasPermissionClaim(user, ResourceType.ApplicationFiles, PermissionConstants.AnyResourceKey, AccessType.Read);

    /// <summary>
    /// Returns true when the user can write files for the specified application (exact permission or Admin only).
    /// </summary>
    public static bool CanWriteApplicationFiles(ClaimsPrincipal user, string applicationId) =>
        HasFullAdminAccess(user)
        || HasPermissionClaim(user, ResourceType.ApplicationFiles, applicationId, AccessType.Write);

    /// <summary>
    /// Returns true when the user can delete files for the specified application (exact permission or Admin only).
    /// </summary>
    public static bool CanDeleteApplicationFiles(ClaimsPrincipal user, string applicationId) =>
        HasFullAdminAccess(user)
        || HasPermissionClaim(user, ResourceType.ApplicationFiles, applicationId, AccessType.Delete);

    /// <summary>
    /// Returns true when the user has an exact or wildcard permission claim for the resource.
    /// </summary>
    public static bool HasPermissionClaim(
        ClaimsPrincipal user,
        ResourceType resourceType,
        string resourceId,
        AccessType accessType)
    {
        var expected = FormatPermissionClaim(resourceType, resourceId, accessType);
        return user.Claims.Any(c =>
            c.Type == PermissionClaimType
            && string.Equals(c.Value, expected, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Returns true when the user has at least one permission claim for the resource type and access level.
    /// </summary>
    public static bool HasAnyPermissionClaim(
        ClaimsPrincipal user,
        ResourceType resourceType,
        AccessType accessType)
    {
        var prefix = $"{resourceType}:";
        var suffix = $":{accessType}";

        return user.Claims.Any(c =>
            c.Type == PermissionClaimType
            && c.Value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            && c.Value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Formats a permission claim value.
    /// </summary>
    public static string FormatPermissionClaim(ResourceType resourceType, string resourceId, AccessType accessType) =>
        $"{resourceType}:{resourceId}:{accessType}";
}
