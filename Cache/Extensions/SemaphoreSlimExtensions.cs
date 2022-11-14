namespace Cache.Extensions;

public static class SemaphoreSlimExtensions
{
    public static async Task WaitAndReleaseAsync(this SemaphoreSlim semaphore, Func<Task> awaitedOperationFunc, CancellationToken cancellationToken = default)
    {
        await semaphore.WaitAndReleaseAsync<object?>(async () =>
        {
            await awaitedOperationFunc();
            return null;
        }, cancellationToken: cancellationToken);
    }
    
    public static async Task<T> WaitAndReleaseAsync<T>(this SemaphoreSlim semaphore, Func<Task<T>> awaitedOperationFunc, CancellationToken cancellationToken = default)
    {
        try
        {
            await semaphore.WaitAsync(cancellationToken);
            return await awaitedOperationFunc();
        }
        finally
        {
            semaphore.Release();
        }
    }
}