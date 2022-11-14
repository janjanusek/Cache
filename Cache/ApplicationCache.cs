using Cache.Base;
using Cache.Extensions;

namespace Cache;

public class ApplicationCache : IApplicationCache, IAsyncDisposable
{
    private const int SINGLE_TASK_ACCESS = 1;
    private readonly Dictionary<string, CacheItem> _cachedItems = new();
    private readonly SemaphoreSlim _access = new(SINGLE_TASK_ACCESS);

    public Task<T?> GetOrAddValueByKeyAsync<T>(ICache.Request request, CancellationToken cancellationToken = default) where T : class
    {
        return _access.WaitAndReleaseAsync(async () =>
        {
            if (_cachedItems.TryGetValue(request.Key, out var cValue) == false)
                _cachedItems.Add(request.Key, cValue = new CacheItem(request));
            return await cValue.GetOrFetchData<T?>(cancellationToken);
        }, cancellationToken);
    }

    public Task<T?> GetValueByKeyAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        return _access.WaitAndReleaseAsync(() =>
        {
            _cachedItems.TryGetValue(key, out var value);
            return Task.FromResult(value as T);
        }, cancellationToken);
    }

    public Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        return _access.WaitAndReleaseAsync(async () =>
        {
            var removed = _cachedItems.TryGetValue(key, out var value);
            if (removed)
                await value!.DisposeAsync();
            return removed;
        }, cancellationToken);
    }

    public Task<ICollection<string>> GetKeysAsync(CancellationToken cancellationToken = default)
    {
        return _access.WaitAndReleaseAsync(() => Task.FromResult((ICollection<string>)_cachedItems.Keys.ToList()), cancellationToken);
    }

    public async ValueTask DisposeAsync() => await Task.WhenAll(_cachedItems.Select(i => i.Value.DisposeAsync().AsTask()));
}