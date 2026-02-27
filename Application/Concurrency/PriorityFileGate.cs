namespace Application.Concurrency;

/// <summary>
/// Global gate that blocks non-priority file transfers until all priority files
/// across ALL running jobs have been processed. Uses an internal counter with
/// an AsyncManualResetEvent: the gate is open (non-priority can proceed) when
/// the counter is zero, and closed when priority files are registered.
/// </summary>
public sealed class PriorityFileGate
{
    private readonly object _lock = new();
    private readonly AsyncManualResetEvent _gate = new(initialState: true); // open by default
    private int _remainingPriorityFiles;

    /// <summary>
    /// Registers a count of priority files to be processed. Closes the gate
    /// if it was open. Thread-safe: multiple jobs can register concurrently.
    /// </summary>
    public void RegisterPriorityFiles(int count)
    {
        if (count <= 0) return;

        lock (_lock)
        {
            _remainingPriorityFiles += count;
            _gate.Reset(); // close: non-priority files must wait
        }
    }

    /// <summary>
    /// Signals that one priority file has been processed (success or error).
    /// Opens the gate when the counter reaches zero.
    /// </summary>
    public void PriorityFileCompleted()
    {
        lock (_lock)
        {
            if (_remainingPriorityFiles <= 0) return; // guard against double-decrement
            _remainingPriorityFiles--;
            if (_remainingPriorityFiles == 0)
                _gate.Set(); // open: non-priority files can proceed
        }
    }

    /// <summary>
    /// Releases a number of priority files at once (e.g., on job stop with
    /// unprocessed priority files remaining).
    /// </summary>
    public void ReleasePriorityFiles(int count)
    {
        if (count <= 0) return;

        lock (_lock)
        {
            _remainingPriorityFiles = Math.Max(0, _remainingPriorityFiles - count);
            if (_remainingPriorityFiles == 0)
                _gate.Set();
        }
    }

    /// <summary>
    /// Awaits until all priority files across all jobs have been processed.
    /// Returns immediately if no priority files are registered.
    /// </summary>
    public Task WaitForPriorityCompletionAsync(CancellationToken ct = default)
    {
        return _gate.WaitAsync(ct);
    }

    /// <summary>
    /// Returns the current count of remaining priority files. For diagnostics only.
    /// </summary>
    public int RemainingCount
    {
        get
        {
            lock (_lock)
            {
                return _remainingPriorityFiles;
            }
        }
    }
}
