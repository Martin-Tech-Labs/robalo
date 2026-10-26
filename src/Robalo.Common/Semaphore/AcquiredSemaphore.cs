namespace Robalo.Common.Semaphore;

public sealed class AcquiredSemaphore(SemaphoreWrapper semaphoreWrapper) : IDisposable
{
    readonly SemaphoreWrapper _semaphore = semaphoreWrapper;
    public void Dispose() => _semaphore.Release();
}