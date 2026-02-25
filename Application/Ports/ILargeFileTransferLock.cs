namespace Application.Ports;

public interface ILargeFileTransferLock
{
    Task AcquireAsync(CancellationToken ct = default);
    void Release();
}
