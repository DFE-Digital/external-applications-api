using GovUK.Dfe.CoreLibs.Caching.Helpers;
using GovUK.Dfe.CoreLibs.Caching.Interfaces;
using DfE.ExternalApplications.Application.Common;
using DfE.ExternalApplications.Domain.Tenancy;
using DfE.ExternalApplications.Domain.ValueObjects;

namespace DfE.ExternalApplications.Application.Services;

/// <summary>
/// Removes tenant-scoped Redis cache entries used when building user permission claims.
/// </summary>
public sealed class UserPermissionCacheInvalidator(
    ICacheService<IRedisCacheType> cacheService,
    ITenantContextAccessor tenantContextAccessor) : IUserPermissionCacheInvalidator
{
    /// <inheritdoc />
    public void InvalidateForUser(string email, UserId userId)
    {
        ArgumentNullException.ThrowIfNull(userId);

        if (!string.IsNullOrWhiteSpace(email))
        {
            var userClaimsKey = TenantCacheKeyHelper.CreateTenantScopedKey(
                tenantContextAccessor,
                $"UserClaims_{CacheKeyHelper.GenerateHashedCacheKey(email.ToLower())}");
            cacheService.Remove(userClaimsKey);
        }

        var userIdHash = CacheKeyHelper.GenerateHashedCacheKey(userId.Value.ToString());

        cacheService.Remove(TenantCacheKeyHelper.CreateTenantScopedKey(
            tenantContextAccessor,
            $"Permissions_All_UserId_{userIdHash}"));

        cacheService.Remove(TenantCacheKeyHelper.CreateTenantScopedKey(
            tenantContextAccessor,
            $"Template_Permissions_ByUiD_{userIdHash}"));
    }
}
