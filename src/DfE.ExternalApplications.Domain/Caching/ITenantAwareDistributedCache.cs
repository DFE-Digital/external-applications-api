using Microsoft.Extensions.Caching.Distributed;

namespace DfE.ExternalApplications.Domain.Caching;

/// <summary>
/// A tenant-aware distributed cache that automatically prefixes all cache keys
/// with the current tenant identifier, ensuring tenant isolation in shared Redis.
/// </summary>
public interface ITenantAwareDistributedCache : IDistributedCache
{
    /// <summary>
    /// Gets the tenant-prefixed version of the given key.
    /// Useful for debugging or when you need to know the actual key being used.
    /// </summary>
    /// <param name="key">The original cache key.</param>
    /// <returns>The tenant-prefixed key.</returns>
    string GetTenantPrefixedKey(string key);
}
