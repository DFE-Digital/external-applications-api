using DfE.ExternalApplications.Domain.Tenancy;

namespace DfE.ExternalApplications.Application.Common;

/// <summary>
/// Helper for creating tenant-scoped cache keys.
/// Use this to prefix cache keys with the current tenant ID for multi-tenant isolation.
/// </summary>
public static class TenantCacheKeyHelper
{
    /// <summary>
    /// Creates a tenant-scoped cache key by prefixing with the tenant ID.
    /// </summary>
    /// <param name="tenantContextAccessor">The tenant context accessor.</param>
    /// <param name="baseCacheKey">The original cache key.</param>
    /// <returns>A cache key prefixed with the tenant ID, or the original key if no tenant context.</returns>
    /// <example>
    /// Input: "Applications_ForUser_abc123"
    /// Output: "t:550e8400-e29b-41d4-a716-446655440000:Applications_ForUser_abc123"
    /// </example>
    public static string CreateTenantScopedKey(ITenantContextAccessor? tenantContextAccessor, string baseCacheKey)
    {
        var tenant = tenantContextAccessor?.CurrentTenant;
        if (tenant == null)
        {
            return baseCacheKey;
        }
        
        return $"t:{tenant.Id}:{baseCacheKey}";
    }
}
