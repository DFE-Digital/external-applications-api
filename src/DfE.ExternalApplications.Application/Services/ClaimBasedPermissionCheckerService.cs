using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.ExternalApplications.Domain.Common;
using DfE.ExternalApplications.Domain.Services;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace DfE.ExternalApplications.Application.Services;

/// <summary>
/// Implementation of IPermissionCheckerService that checks permissions based on claims in the current user's ClaimsPrincipal
/// </summary>
public sealed class ClaimBasedPermissionCheckerService(IHttpContextAccessor httpContextAccessor)
    : IPermissionCheckerService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

    /// <inheritdoc />
    public bool HasPermission(ResourceType resourceType, string resourceId, AccessType accessType)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return false;

        if (PermissionClaimEvaluator.HasFullAdminAccess(user))
            return true;

        if (accessType == AccessType.Read && IsCaseworkerReadResource(user, resourceType))
            return true;

        if (PermissionClaimEvaluator.HasPermissionClaim(user, resourceType, resourceId, accessType))
            return true;

        if (accessType == AccessType.Read
            && PermissionClaimEvaluator.HasPermissionClaim(user, resourceType, PermissionConstants.AnyResourceKey, AccessType.Read))
            return true;

        return false;
    }

    /// <inheritdoc />
    public bool HasTemplatePermission(string templateId, AccessType accessType)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return false;

        if (PermissionClaimEvaluator.HasFullAdminAccess(user))
            return true;

        return PermissionClaimEvaluator.HasPermissionClaim(user, ResourceType.Template, templateId, accessType);
    }
    
    /// <inheritdoc />
    public bool HasAnyPermission(ResourceType resourceType, AccessType accessType)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return false;

        if (PermissionClaimEvaluator.CanReadAllApplications(user) && resourceType == ResourceType.Application && accessType == AccessType.Read)
            return true;

        return PermissionClaimEvaluator.HasAnyPermissionClaim(user, resourceType, accessType);
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> GetResourceIdsWithPermission(ResourceType resourceType, AccessType accessType)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return Array.Empty<string>();

        return PermissionClaimEvaluator.HasAnyPermissionClaim(user, resourceType, accessType)
            ? user.Claims
                .Where(c => c.Type == PermissionClaimEvaluator.PermissionClaimType
                    && c.Value.StartsWith($"{resourceType}:", StringComparison.OrdinalIgnoreCase)
                    && c.Value.EndsWith($":{accessType}", StringComparison.OrdinalIgnoreCase))
                .Select(c => c.Value.Split(':')[1])
                .ToList()
                .AsReadOnly()
            : Array.Empty<string>();
    }

    /// <inheritdoc />
    public bool IsApplicationOwner(string applicationId)
    {
        if (string.IsNullOrWhiteSpace(applicationId))
            return false;

        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return false;

        if (PermissionClaimEvaluator.HasFullAdminAccess(user))
            return true;

        return PermissionClaimEvaluator.CanWriteApplication(user, applicationId);
    }

    /// <inheritdoc />
    public bool IsApplicationOwner(DfE.ExternalApplications.Domain.Entities.Application application, string currentUserId)
    {
        if (PermissionClaimEvaluator.HasFullAdminAccess(_httpContextAccessor.HttpContext?.User!))
            return true;

        if (application == null) return false;
        if (string.IsNullOrEmpty(currentUserId)) return false;

        return application.CreatedBy.Value.ToString() == currentUserId;
    }

    /// <inheritdoc />
    public bool CanManageContributors(string applicationId)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return false;

        if (PermissionClaimEvaluator.HasFullAdminAccess(user))
            return true;

        return IsApplicationOwner(applicationId);
    }

    /// <inheritdoc />
    public bool IsAdmin()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return false;

        return PermissionClaimEvaluator.HasFullAdminAccess(user);
    }

    /// <inheritdoc />
    public bool IsCaseworker()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return false;

        return user.IsInRole(RoleNames.Caseworker);
    }

    /// <inheritdoc />
    public bool CanReadAllApplications()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return false;

        return PermissionClaimEvaluator.CanReadAllApplications(user);
    }

    private static bool IsCaseworkerReadResource(ClaimsPrincipal user, ResourceType resourceType) =>
        user.IsInRole(RoleNames.Caseworker)
        && resourceType is ResourceType.Application or ResourceType.ApplicationFiles;
}
