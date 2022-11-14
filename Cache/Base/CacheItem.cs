using System.Timers;
using Cache.Extensions;
using Timer = System.Timers.Timer;

namespace Cache.Base;

public sealed class CacheItem : IAsyncDisposable
{
    private const int SINGLE_TASK_ALLOWED = 1;
    private readonly SemaphoreSlim _access = new(SINGLE_TASK_ALLOWED);
    private readonly ICache.Request _request;
    private readonly CancellationTokenSource _tokenSource;
    private readonly Timer? _refresh;
    private object? _localValue;
    private DateTime _timestamp;
    private int _refreshCounter;

    public CacheItem(ICache.Request request)
    {
        _request = request;
        _tokenSource = new CancellationTokenSource();
        _timestamp = DateTime.MinValue;
        _refreshCounter = 0;
        if (_request.RefreshLimit > 0)
        {
            _refresh = new Timer(_request.Lifetime.TotalMilliseconds)
            {
                AutoReset = true,
                Enabled = true
            };
            _refresh.Elapsed += RefreshHandler;
        }
    }

    public async Task<T?> GetOrFetchData<T>(CancellationToken cancellationToken = default) where T : class?
    {
        bool IsExpired() => _timestamp.Add(_request.Lifetime) <= DateTime.UtcNow;
        var data = await _access.WaitAndReleaseAsync(async () =>
        {
            if (IsExpired())
            {
                await TryDisposeAsync();
                _localValue = await _request.AsObjTask(cancellationToken);
                _timestamp = DateTime.UtcNow;
            }

            return _localValue as T;
        }, cancellationToken);
        TryStartRefresh();
        return data;
    }

    public async ValueTask DisposeAsync() => await TryDisposeAsync();

    private async Task TryDisposeAsync()
    {
        if (_refresh != null)
        {
            _tokenSource.Cancel();
            _refresh.Stop();
            _refresh.Elapsed -= RefreshHandler;
            _refresh.Dispose();
        }

        if (!_request.AllowDispose)
            return;

        switch (_localValue)
        {
            case IAsyncDisposable asyncDisposable:
                await asyncDisposable.DisposeAsync();
                break;
            case IDisposable disposable:
                disposable.Dispose();
                break;
        }
    }

    private void TryStartRefresh()
    {
        if (_refresh is { Enabled: false })
        {
            _refreshCounter = 0;
            _refresh.Start();
        }
    }

    private async void RefreshHandler(object? sender, ElapsedEventArgs e)
    {
        try
        {
            await GetOrFetchData<object>(_tokenSource.Token);
            _refreshCounter++;
            if (_refreshCounter == _request.RefreshLimit)
                _refresh!.Stop();
        }
        catch
        {
            _refresh!.Stop();
        }
    }
}