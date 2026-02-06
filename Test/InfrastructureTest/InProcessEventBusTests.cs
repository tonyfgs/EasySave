using Application.Events;
using Infrastructure;
using Moq;

namespace InfrastructureTest;

public class InProcessEventBusTests
{
    [Fact]
    public void Publish_WithSubscribedHandler_ShouldCallHandler()
    {
        var bus = new InProcessEventBus();
        var handler = new Mock<IEventHandler<TestEvent>>();
        var evt = new TestEvent("hello");

        bus.Subscribe(handler.Object);
        bus.Publish(evt);

        handler.Verify(h => h.Handle(evt), Times.Once);
    }

    [Fact]
    public void Publish_WithNoSubscribers_ShouldNotThrow()
    {
        var bus = new InProcessEventBus();

        var exception = Record.Exception(() => bus.Publish(new TestEvent("hello")));

        Assert.Null(exception);
    }

    [Fact]
    public void Publish_WithMultipleHandlers_ShouldCallAll()
    {
        var bus = new InProcessEventBus();
        var handler1 = new Mock<IEventHandler<TestEvent>>();
        var handler2 = new Mock<IEventHandler<TestEvent>>();
        var evt = new TestEvent("hello");

        bus.Subscribe(handler1.Object);
        bus.Subscribe(handler2.Object);
        bus.Publish(evt);

        handler1.Verify(h => h.Handle(evt), Times.Once);
        handler2.Verify(h => h.Handle(evt), Times.Once);
    }

    [Fact]
    public void Publish_DifferentEventTypes_ShouldOnlyCallMatchingHandlers()
    {
        var bus = new InProcessEventBus();
        var testHandler = new Mock<IEventHandler<TestEvent>>();
        var otherHandler = new Mock<IEventHandler<OtherEvent>>();

        bus.Subscribe(testHandler.Object);
        bus.Subscribe(otherHandler.Object);
        bus.Publish(new TestEvent("hello"));

        testHandler.Verify(h => h.Handle(It.IsAny<TestEvent>()), Times.Once);
        otherHandler.Verify(h => h.Handle(It.IsAny<OtherEvent>()), Times.Never);
    }

    public record TestEvent(string Message);
    public record OtherEvent(int Value);
}
