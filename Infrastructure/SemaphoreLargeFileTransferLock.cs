using Application.Ports;

namespace Infrastructure;

public sealed class SemaphoreLargeFileTransferLock : ILargeFileTransferLock
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public Task AcquireAsync(CancellationToken ct = default)
        => _semaphore.WaitAsync(ct);

    public void Release()
        => _semaphore.Release();
}
