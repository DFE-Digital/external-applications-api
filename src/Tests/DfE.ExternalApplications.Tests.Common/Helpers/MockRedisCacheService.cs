using GovUK.Dfe.CoreLibs.Caching.Interfaces;

namespace DfE.ExternalApplications.Tests.Common.Helpers;

/// <summary>
/// In-memory pass-through cache for integration tests, removing the dependency on a running Redis server.
/// Always executes the factory function (no actual caching) which is ideal for test isolation.
/// </summary>
public class MockRedisCacheService : IAdvancedRedisCacheService, ICacheService<IRedisCacheType>
{
    private readonly Dictionary<string, byte[]> _rawStore = new();

    public Type CacheType => typeof(IRedisCacheType);

    public async Task<T> GetOrAddAsync<T>(string cacheKey, Func<Task<T>> fetchFunction, string methodName,
        CancellationToken cancellationToken = default)
    {
        return await fetchFunction();
    }

    public Task<T?> GetAsync<T>(string cacheKey) => Task.FromResult<T?>(default);

    public void Remove(string cacheKey)
    {
        _rawStore.Remove(cacheKey);
    }

    public Task SetRawAsync(string cacheKey, byte[] value, TimeSpan expiry)
    {
        _rawStore[cacheKey] = value;
        return Task.CompletedTask;
    }

    public Task<byte[]?> GetRawAsync(string cacheKey)
    {
        _rawStore.TryGetValue(cacheKey, out var value);
        return Task.FromResult(value);
    }

    public Task RemoveAsync(string cacheKey)
    {
        _rawStore.Remove(cacheKey);
        return Task.CompletedTask;
    }

    public Task RemoveByPatternAsync(string pattern)
    {
        var keysToRemove = _rawStore.Keys
            .Where(k => k.StartsWith(pattern.TrimEnd('*')))
            .ToList();
        foreach (var key in keysToRemove)
            _rawStore.Remove(key);
        return Task.CompletedTask;
    }
}
