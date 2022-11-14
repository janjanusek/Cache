namespace Cache.Base
{
    public interface ICache
    {
        Task<T?> GetOrAddValueByKeyAsync<T>(Request request, CancellationToken cancellationToken = default) where T : class;
        Task<T?> GetValueByKeyAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
        Task<ICollection<string>> GetKeysAsync(CancellationToken cancellationToken = default);
        Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);

        public class Request
        {
            public delegate Task<T?> GetValueFuncAsyncDelegate<T>(CancellationToken cancellationToken = default);
            public string Key { get; init; } = null!;
            public TimeSpan Lifetime { get; init; }
            public bool AllowDispose { get; init; }
            public int? RefreshLimit { get; init; }

            public GetValueFuncAsyncDelegate<object?>? AsyncDelegate { private get; init; }

            public virtual Task<object?> AsObjTask(CancellationToken cancellationToken = default) => AsyncDelegate!(cancellationToken);

            public static Request<T> DefineRequest<T>(Request request, GetValueFuncAsyncDelegate<T> asyncDelegate) where T : class => new()
            {
                Key = request.Key,
                Lifetime = request.Lifetime,
                AllowDispose = request.AllowDispose,
                RefreshLimit = request.RefreshLimit,
                AsyncDelegate = asyncDelegate!
            };
        }

        public sealed class Request<T> : Request where T : class
        {
            public new GetValueFuncAsyncDelegate<T?> AsyncDelegate { private get; init; } = null!;

            public override Task<object?> AsObjTask(CancellationToken cancellationToken = default) 
                => AsyncDelegate.Invoke(cancellationToken).ContinueWith(t => t.Result as object, cancellationToken);
        }
    }

    public interface IApplicationCache : ICache
    {
        
    }
}