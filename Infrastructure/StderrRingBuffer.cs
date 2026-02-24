namespace Infrastructure;

/// <summary>
/// Thread-safe fixed-capacity circular buffer for stderr lines.
/// Event handlers fire on thread-pool threads, so all access is lock-protected.
/// </summary>
public sealed class StderrRingBuffer
{
    private readonly int _capacity;
    private readonly Queue<string> _buffer;
    private readonly object _lock = new();

    public StderrRingBuffer(int capacity = 200)
    {
        _capacity = capacity;
        _buffer = new Queue<string>(capacity);
    }

    public void Append(string line)
    {
        lock (_lock)
        {
            if (_buffer.Count >= _capacity)
                _buffer.Dequeue();
            _buffer.Enqueue(line);
        }
    }

    public IReadOnlyList<string> GetLines()
    {
        lock (_lock)
        {
            return _buffer.ToList().AsReadOnly();
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _buffer.Clear();
        }
    }

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _buffer.Count;
            }
        }
    }
}
