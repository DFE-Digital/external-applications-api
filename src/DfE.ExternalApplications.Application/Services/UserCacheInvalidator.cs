using GovUK.Dfe.CoreLibs.Caching.Helpers;
using GovUK.Dfe.CoreLibs.Caching.Interfaces;
using DfE.ExternalApplications.Application.Common;
using DfE.ExternalApplications.Domain.Tenancy;
using DfE.ExternalApplications.Domain.ValueObjects;

namespace DfE.ExternalApplications.Application.Services;

/// <summary>
/// Removes tenant-scoped Redis cache entries used for user permissions and application listings.
/// </summary>
public sealed class UserCacheInvalidator(
    ICacheService<IRedisCacheType> cacheService,
    IAdvancedRedisCacheService advancedRedisCacheService,
    ITenantContextAccessor tenantContextAccessor) : IUserCacheInvalidator
{
    /// <inheritdoc />
    public async Task InvalidateForUserAsync(
        string? email,
        string? externalProviderId,
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);

        if (!string.IsNullOrWhiteSpace(email))
        {
            var userClaimsKey = TenantCacheKeyHelper.CreateTenantScopedKey(
                tenantContextAccessor,
                $"UserClaims_{CacheKeyHelper.GenerateHashedCacheKey(email.ToLower())}");
            cacheService.Remove(userClaimsKey);

            var emailListingPattern = TenantCacheKeyHelper.CreateTenantScopedKey(
                tenantContextAccessor,
                $"Applications_ForUser_{CacheKeyHelper.GenerateHashedCacheKey(email)}*");
            await advancedRedisCacheService.RemoveByPatternAsync(emailListingPattern);
        }

        var userIdHash = CacheKeyHelper.GenerateHashedCacheKey(userId.Value.ToString());

        cacheService.Remove(TenantCacheKeyHelper.CreateTenantScopedKey(
            tenantContextAccessor,
            $"Permissions_All_UserId_{userIdHash}"));

        cacheService.Remove(TenantCacheKeyHelper.CreateTenantScopedKey(
            tenantContextAccessor,
            $"Template_Permissions_ByUiD_{userIdHash}"));

        if (!string.IsNullOrWhiteSpace(externalProviderId))
        {
            var externalIdListingPattern = TenantCacheKeyHelper.CreateTenantScopedKey(
                tenantContextAccessor,
                $"Applications_ForUserExternal_{CacheKeyHelper.GenerateHashedCacheKey(externalProviderId)}*");
            await advancedRedisCacheService.RemoveByPatternAsync(externalIdListingPattern);
        }
    }
}
