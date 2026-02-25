using Application.Events;

namespace Infrastructure;

public class InProcessEventBus : IEventBus
{
    private readonly Dictionary<Type, List<object>> _handlers = new();
    private readonly object _lock = new();

    public void Subscribe<T>(IEventHandler<T> handler)
    {
        var key = typeof(T);
        lock (_lock)
        {
            if (!_handlers.ContainsKey(key))
                _handlers[key] = new List<object>();
            _handlers[key].Add(handler);
        }
    }

    public void Publish<T>(T @event)
    {
        var key = typeof(T);
        List<object>? snapshot;
        lock (_lock)
        {
            if (!_handlers.TryGetValue(key, out var handlers))
                return;
            snapshot = new List<object>(handlers);
        }

        foreach (var handler in snapshot)
            ((IEventHandler<T>)handler).Handle(@event);
    }
}
