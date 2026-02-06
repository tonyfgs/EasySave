using Application.Events;

namespace Infrastructure;

public class InProcessEventBus : IEventBus
{
    private readonly Dictionary<Type, List<object>> _handlers = new();

    public void Subscribe<T>(IEventHandler<T> handler)
    {
        var key = typeof(T);
        if (!_handlers.ContainsKey(key))
            _handlers[key] = new List<object>();
        _handlers[key].Add(handler);
    }

    public void Publish<T>(T @event)
    {
        var key = typeof(T);
        if (!_handlers.TryGetValue(key, out var handlers))
            return;

        foreach (var handler in handlers)
        {
            ((IEventHandler<T>)handler).Handle(@event);
        }
    }
}
