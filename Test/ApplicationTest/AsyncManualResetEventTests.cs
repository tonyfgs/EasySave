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
}
