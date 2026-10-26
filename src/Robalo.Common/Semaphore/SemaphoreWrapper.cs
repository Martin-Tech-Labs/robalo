namespace Robalo.Common.Semaphore;

public sealed class SemaphoreWrapper(SemaphoreSlim semaphore) : IDisposable
{
    private readonly SemaphoreSlim _semaphore = semaphore;
    public void Dispose() => _semaphore.Dispose();
    public void Release() => _semaphore.Release();

    public async Task<IDisposable> Wait(CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        return new AcquiredSemaphore(this);
    }
}