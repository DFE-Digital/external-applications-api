using DfE.ExternalApplications.Domain.Caching;
using DfE.ExternalApplications.Domain.Tenancy;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace DfE.ExternalApplications.Infrastructure.Caching;

/// <summary>
/// Wraps an IDistributedCache to automatically prefix all keys with the current tenant ID.
/// This ensures complete tenant isolation when using a shared Redis instance.
/// 
/// Key format: tenant:{tenantId}:{originalKey}
/// Example: tenant:550e8400-e29b-41d4-a716-446655440000:hub:ticket:abc123
/// </summary>
public class TenantAwareDistributedCache(
    IDistributedCache innerCache,
    ITenantContextAccessor tenantContextAccessor,
    ILogger<TenantAwareDistributedCache> logger)
    : ITenantAwareDistributedCache
{
    private readonly IDistributedCache _innerCache = innerCache ?? throw new ArgumentNullException(nameof(innerCache));
    private readonly ITenantContextAccessor _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
    private readonly ILogger<TenantAwareDistributedCache> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Prefix format for tenant-scoped keys.
    /// Using a short prefix to minimize key length while maintaining clarity.
    /// </summary>
    private const string TenantKeyPrefix = "t";

    /// <inheritdoc />
    public string GetTenantPrefixedKey(string key)
    {
        var tenant = _tenantContextAccessor.CurrentTenant;
        
        if (tenant == null)
        {
            _logger.LogWarning(
                ">>>>>>>>>> CACHE >>> No tenant context available when accessing cache key '{Key}'. " +
                "Using unprefixed key. TenantContextAccessor type: {Type}, HashCode: {HashCode}",
                key,
                _tenantContextAccessor.GetType().FullName,
                _tenantContextAccessor.GetHashCode());
            return key;
        }

        // Format: t:{tenantId}:{key}
        // Example: t:550e8400-e29b-41d4-a716-446655440000:hub:ticket:abc123
        var prefixedKey = $"{TenantKeyPrefix}:{tenant.Id}:{key}";
        _logger.LogDebug(
            ">>>>>>>>>> CACHE >>> Tenant prefix applied. Original: '{OriginalKey}', Prefixed: '{PrefixedKey}', TenantId: {TenantId}",
            key, prefixedKey, tenant.Id);
        return prefixedKey;
    }

    /// <inheritdoc />
    public byte[]? Get(string key)
    {
        var prefixedKey = GetTenantPrefixedKey(key);
        return _innerCache.Get(prefixedKey);
    }

    /// <inheritdoc />
    public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        var prefixedKey = GetTenantPrefixedKey(key);
        return await _innerCache.GetAsync(prefixedKey, token);
    }

    /// <inheritdoc />
    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        var prefixedKey = GetTenantPrefixedKey(key);
        _innerCache.Set(prefixedKey, value, options);
    }

    /// <inheritdoc />
    public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        var prefixedKey = GetTenantPrefixedKey(key);
        await _innerCache.SetAsync(prefixedKey, value, options, token);
    }

    /// <inheritdoc />
    public void Refresh(string key)
    {
        var prefixedKey = GetTenantPrefixedKey(key);
        _innerCache.Refresh(prefixedKey);
    }

    /// <inheritdoc />
    public async Task RefreshAsync(string key, CancellationToken token = default)
    {
        var prefixedKey = GetTenantPrefixedKey(key);
        await _innerCache.RefreshAsync(prefixedKey, token);
    }

    /// <inheritdoc />
    public void Remove(string key)
    {
        var prefixedKey = GetTenantPrefixedKey(key);
        _innerCache.Remove(prefixedKey);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        var prefixedKey = GetTenantPrefixedKey(key);
        await _innerCache.RemoveAsync(prefixedKey, token);
    }
}
