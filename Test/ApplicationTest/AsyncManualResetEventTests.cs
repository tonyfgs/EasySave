using Application.Concurrency;

namespace ApplicationTest;

public class AsyncManualResetEventTests
{
    [Fact]
    public void Constructor_WithDefaultState_IsSetTrue()
    {
        var mre = new AsyncManualResetEvent();

        Assert.True(mre.IsSet);
    }

    [Fact]
    public void Constructor_WithFalseState_IsSetFalse()
    {
        var mre = new AsyncManualResetEvent(initialState: false);

        Assert.False(mre.IsSet);
    }

    [Fact]
    public async Task WaitAsync_WhenSignaled_ReturnsCompletedTask()
    {
        var mre = new AsyncManualResetEvent();

        var waitTask = mre.WaitAsync();

        Assert.True(waitTask.IsCompleted);
        await waitTask;
    }

    [Fact]
    public async Task WaitAsync_WhenUnsignaled_DoesNotCompleteWithin200ms()
    {
        var mre = new AsyncManualResetEvent(initialState: false);

        var waitTask = mre.WaitAsync();

        var completedTask = await Task.WhenAny(waitTask, Task.Delay(200));
        Assert.NotEqual(waitTask, completedTask);
    }

    [Fact]
    public async Task Set_WhenWaiterPending_UnblocksWithin100ms()
    {
        var mre = new AsyncManualResetEvent(initialState: false);
        var waitTask = mre.WaitAsync();

        mre.Set();

        var completedTask = await Task.WhenAny(waitTask, Task.Delay(100));
        Assert.Equal(waitTask, completedTask);
        await waitTask;
    }

    [Fact]
    public async Task WaitAsync_FullSignalCycle_SuspendsAfterResetAndCompletesAfterSet()
    {
        var mre = new AsyncManualResetEvent();
        Assert.True(mre.IsSet);

        mre.Reset();
        var waitTask = mre.WaitAsync();

        var race1 = await Task.WhenAny(waitTask, Task.Delay(200));
        Assert.NotEqual(waitTask, race1);

        mre.Set();

        var race2 = await Task.WhenAny(waitTask, Task.Delay(100));
        Assert.Equal(waitTask, race2);
        await waitTask;
    }

    [Fact]
    public void IsSet_AfterSetAndReset_ReflectsCurrentState()
    {
        var mre = new AsyncManualResetEvent();
        Assert.True(mre.IsSet);

        mre.Reset();
        Assert.False(mre.IsSet);

        mre.Set();
        Assert.True(mre.IsSet);
    }

    [Fact]
    public async Task Set_CalledTwice_RemainsSignaled()
    {
        var mre = new AsyncManualResetEvent();

        mre.Set();
        mre.Set();

        Assert.True(mre.IsSet);
        var waitTask = mre.WaitAsync();
        Assert.True(waitTask.IsCompleted);
        await waitTask;
    }

    [Fact]
    public async Task Reset_CalledTwice_RemainsUnsignaled()
    {
        var mre = new AsyncManualResetEvent(initialState: false);

        mre.Reset();
        mre.Reset();

        Assert.False(mre.IsSet);
        var waitTask = mre.WaitAsync();
        var completedTask = await Task.WhenAny(waitTask, Task.Delay(200));
        Assert.NotEqual(waitTask, completedTask);
    }
}
