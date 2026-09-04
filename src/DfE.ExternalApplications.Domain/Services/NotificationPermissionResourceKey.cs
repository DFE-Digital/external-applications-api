using System.Security.Claims;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;

namespace DfE.ExternalApplications.Domain.Services;

/// <summary>
/// Builds and matches Notifications permission resource keys.
/// Keys are tenant-scoped as <c>{tenantId}:{principalId}</c> (e.g. email or app id),
/// with legacy principalId-only keys still accepted.
/// </summary>
public static class NotificationPermissionResourceKey
{
    public static string Create(Guid tenantId, string principalId) =>
        $"{tenantId}:{principalId}";

    /// <summary>
    /// Candidate resource keys to check against permission claims for Notifications only.
    /// </summary>
    public static IEnumerable<string> CandidateKeys(string principalId, Guid? tenantId)
    {
        if (string.IsNullOrWhiteSpace(principalId))
            yield break;

        yield return principalId;

        if (tenantId is null)
            yield break;

        // Avoid double-prefixing when the caller already passed tenantId:principalId
        if (principalId.StartsWith($"{tenantId.Value}:", StringComparison.OrdinalIgnoreCase))
            yield break;

        yield return Create(tenantId.Value, principalId);
    }

    public static bool HasMatchingClaim(
        ClaimsPrincipal user,
        string principalId,
        AccessType accessType,
        Guid? tenantId)
    {
        foreach (var key in CandidateKeys(principalId, tenantId))
        {
            if (PermissionClaimEvaluator.HasPermissionClaim(user, ResourceType.Notifications, key, accessType))
                return true;
        }

        // Tenant-scoped DB keys (tenantId:email) must still match when tenant context is
        // missing or uses a different tenant id than the stored resource key.
        return HasClaimForPrincipal(user, principalId, accessType);
    }

    private static bool HasClaimForPrincipal(
        ClaimsPrincipal user,
        string principalId,
        AccessType accessType)
    {
        if (string.IsNullOrWhiteSpace(principalId))
            return false;

        var prefix = $"{ResourceType.Notifications}:";
        var suffix = $":{accessType}";

        foreach (var claim in user.Claims)
        {
            if (claim.Type != PermissionClaimEvaluator.PermissionClaimType)
                continue;

            if (!claim.Value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                || !claim.Value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var resourceKey = claim.Value.Substring(
                prefix.Length,
                claim.Value.Length - prefix.Length - suffix.Length);

            if (resourceKey.Equals(principalId, StringComparison.OrdinalIgnoreCase)
                || resourceKey.EndsWith($":{principalId}", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasMatchingClaim(
        ClaimsPrincipal user,
        string principalId,
        string action,
        Guid? tenantId)
    {
        if (!Enum.TryParse<AccessType>(action, ignoreCase: true, out var accessType))
            return false;

        return HasMatchingClaim(user, principalId, accessType, tenantId);
    }
}
