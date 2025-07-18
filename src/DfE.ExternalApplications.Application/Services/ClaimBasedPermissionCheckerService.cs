using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.ExternalApplications.Domain.Services;
using Microsoft.AspNetCore.Http;

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

        var expectedClaim = FormatPermissionClaim(resourceType, resourceId, accessType);
        return user.Claims.Any(c => 
            c.Type == "permission" && 
            string.Equals(c.Value, expectedClaim, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public bool HasTemplatePermission(string templateId, AccessType accessType)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return false;

        var expectedClaim = FormatPermissionClaim(ResourceType.Template, templateId, accessType);
        return user.Claims.Any(c =>
            c.Type == "permission" &&
            string.Equals(c.Value, expectedClaim, StringComparison.OrdinalIgnoreCase));
    }
    
    /// <inheritdoc />
    public bool HasAnyPermission(ResourceType resourceType, AccessType accessType)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return false;

        var prefix = $"{resourceType}:";
        var suffix = $":{accessType}";

        return user.Claims.Any(c => 
            c.Type == "permission" && 
            c.Value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
            c.Value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> GetResourceIdsWithPermission(ResourceType resourceType, AccessType accessType)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return Array.Empty<string>();

        var prefix = $"{resourceType}:";
        var suffix = $":{accessType}";

        return user.Claims
            .Where(c => c.Type == "permission" &&
                   c.Value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                   c.Value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Value.Split(':')[1])
            .ToList()
            .AsReadOnly();
    }

    /// <inheritdoc />
    public bool IsApplicationOwner(string applicationId)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return false;

        // Check if user has write permission for the application (owner has write access)
        return HasPermission(ResourceType.Application, applicationId, AccessType.Write);
    }

    /// <inheritdoc />
    public bool IsApplicationOwner(DfE.ExternalApplications.Domain.Entities.Application application, string currentUserId)
    {
        if (application == null) return false;
        if (string.IsNullOrEmpty(currentUserId)) return false;

        // Check if the current user is the creator of the application
        return application.CreatedBy.Value.ToString() == currentUserId;
    }

    /// <inheritdoc />
    public bool CanManageContributors(string applicationId)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return false;

        // Admin can manage contributors for any application
        if (IsAdmin()) return true;

        // Application owner can manage contributors
        return IsApplicationOwner(applicationId);
    }

    private static string FormatPermissionClaim(ResourceType resourceType, string resourceId, AccessType accessType)
        => $"{resourceType}:{resourceId}:{accessType}";

    /// <inheritdoc />
    public bool IsAdmin()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return false;

        return user.IsInRole("Admin");
    }
} 