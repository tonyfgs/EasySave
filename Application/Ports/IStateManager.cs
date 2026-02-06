using Application.DTOs;

namespace Application.Ports;

public interface IStateManager
{
    void UpdateState(StateSnapshot snapshot);
    List<StateSnapshot> GetAllStates();
    void ClearAll();
}
