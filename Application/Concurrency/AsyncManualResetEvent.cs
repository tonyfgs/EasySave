namespace Application.Concurrency;

public sealed class AsyncManualResetEvent
{
    private volatile TaskCompletionSource<bool> _tcs;

    public AsyncManualResetEvent(bool initialState = true)
    {
        _tcs = CreateTcs();
        if (initialState)
            _tcs.TrySetResult(true);
    }

    public bool IsSet => _tcs.Task.IsCompleted;

    public Task WaitAsync(CancellationToken ct = default)
    {
        var tcs = _tcs;
        if (tcs.Task.IsCompleted)
            return Task.CompletedTask;

        ct.ThrowIfCancellationRequested();

        if (!ct.CanBeCanceled)
            return tcs.Task;

        return WaitWithCancellationAsync(tcs, ct);
    }

    public void Set() => _tcs.TrySetResult(true);

    public void Reset()
    {
        var current = _tcs;
        if (current.Task.IsCompleted)
            Interlocked.CompareExchange(ref _tcs, CreateTcs(), current);
    }

    private static async Task WaitWithCancellationAsync(
        TaskCompletionSource<bool> tcs, CancellationToken ct)
    {
        var cancelTcs = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        using (ct.Register(s => ((TaskCompletionSource<bool>)s!).TrySetResult(true), cancelTcs))
        {
            var completed = await Task.WhenAny(tcs.Task, cancelTcs.Task).ConfigureAwait(false);
            if (completed == cancelTcs.Task)
                ct.ThrowIfCancellationRequested();
        }
    }

    private static TaskCompletionSource<bool> CreateTcs()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
