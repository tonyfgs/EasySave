using Application.DTOs;
using Application.Events;
using Application.Ports;

namespace Application.Handlers;

public class StateChangedHandler : IEventHandler<StateChangedEvent>
{
    private readonly IStateManager _stateManager;

    public StateChangedHandler(IStateManager stateManager)
    {
        _stateManager = stateManager;
    }

    public void Handle(StateChangedEvent @event)
    {
        _stateManager.UpdateState(@event.Snapshot);
    }
}
