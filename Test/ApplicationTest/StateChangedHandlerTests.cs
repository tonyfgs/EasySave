using Application.DTOs;
using Application.Events;
using Application.Handlers;
using Application.Ports;
using Model;
using Moq;

namespace ApplicationTest;

public class StateChangedHandlerTests
{
    [Fact]
    public void Handle_ShouldCallUpdateStateOnStateManager()
    {
        var mockStateManager = new Mock<IStateManager>();
        var handler = new StateChangedHandler(mockStateManager.Object);
        var snapshot = new StateSnapshot
        {
            Name = "TestJob",
            Timestamp = DateTime.Now,
            State = JobState.Active,
            TotalFilesCount = 10,
            TotalFilesSize = 5000,
            Progress = 50,
            RemainingFilesCount = 5,
            RemainingFilesSize = 2500
        };
        var @event = new StateChangedEvent(snapshot);

        handler.Handle(@event);

        mockStateManager.Verify(sm => sm.UpdateState(snapshot), Times.Once);
    }
}
