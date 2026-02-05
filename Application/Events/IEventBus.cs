namespace Application.Events;

public interface IEventBus
{
    void Publish<T>(T @event);
    void Subscribe<T>(IEventHandler<T> handler);
}
