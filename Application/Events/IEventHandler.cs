namespace Application.Events;

public interface IEventHandler<in T>
{
    void Handle(T @event);
}
