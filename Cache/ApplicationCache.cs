using Cache.Base;

namespace Cache;

public class ApplicationCache : IApplicationCache, IAsyncDisposable
{
    private const int CONCURRENT_TASKS = 1;
    private readonly Dictionary<string, CacheItem> _cachedItems = new();
    private readonly SemaphoreSlim _access = new(CONCURRENT_TASKS);

    public async Task<T?> GetOrAddValueByKeyAsync<T>(ICache.Request request, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            await _access.WaitAsync(cancellationToken);

            if (_cachedItems.TryGetValue(request.Key, out var cValue) == false)
                _cachedItems.TryAdd(request.Key, cValue = new CacheItem(request));

            return await cValue.GetOrFetchData<T?>(cancellationToken);
        }
        finally
        {
            _access.Release();
        }
    }

    public async Task<T?> GetValueByKeyAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            await _access.WaitAsync(cancellationToken);
            _cachedItems.TryGetValue(key, out var value);
            return value as T;
        }
        finally
        {
            _access.Release();
        }
    }

    public async Task<bool> Remove(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _access.WaitAsync(cancellationToken);
            var removed = _cachedItems.TryGetValue(key, out var value);
            if (removed)
                await value!.DisposeAsync();
            return removed;
        }
        finally
        {
            _access.Release();
        }
    }

    public async Task<ICollection<string>> GetKeys(CancellationToken cancellationToken = default)
    {
        try
        {
            await _access.WaitAsync(cancellationToken);
            return _cachedItems.Keys.ToList();
        }
        finally
        {
            _access.Release();
        }
    }

    public async ValueTask DisposeAsync() => await Task.WhenAll(_cachedItems.Select(i => i.Value.DisposeAsync().AsTask()));
}