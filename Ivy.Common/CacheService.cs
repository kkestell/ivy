using System.Collections.Concurrent;

namespace Ivy.Common;

public class CacheService<TKey, TValue>
{
    private readonly ConcurrentDictionary<TKey, TValue> _cache = new();

    public async Task<TValue> GetOrAddAsync(TKey key, Func<Task<TValue>> valueFactory)
    {
        if (_cache.TryGetValue(key, out var value))
        {
            return value;
        }

        value = await valueFactory();
        _cache.TryAdd(key, value);
        return value;
    }
}